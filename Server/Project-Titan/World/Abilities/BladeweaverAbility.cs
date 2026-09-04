using System;
using TitanCore.Core;
using TitanCore.Data.Items;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;

namespace World.Abilities
{
    public class BladeweaverAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Bladeweaver;

        private uint dashEndTime;
        private int spentRage;
        private bool slashHit;
        private bool waitingInvuln;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnHit(EntityState entity, uint time, ref int damageTaken, ushort sourceItem)
        {
            if (sourceItem != 0x2a8) return;

            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            slashHit = true;
            TriggerTalisman(TalismanTrigger.AbilityHit, time, player.position.Value, entity.GetPosition(time), ref damageTaken);
            if (mods.speedOnHit > 0 && mods.speedOnHitMs > 0 && time < dashEndTime)
            {
                int speed = SkillTreeFunctions.ScaleOnUseStat(mods.speedOnHit, spentRage);
                PlayerState.ApplyTimedStatBonus(StatType.Speed, speed, time, (uint)mods.speedOnHitMs);
            }
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override void Tick(uint time)
        {
            if (!waitingInvuln) return;
            if (time < dashEndTime) return;
            waitingInvuln = false;
            var mods = PlayerState.abilityMods;
            if (mods.postDashInvulnMs > 0)
                PlayerState.AddClientStatusEffect(StatusEffect.Invulnerable, time, (uint)mods.postDashInvulnMs);
            TriggerTalisman(TalismanTrigger.AbilityEnd, time, player.position.Value, player.position.Value);
            if (slashHit && mods.rageKeep > 0)
                PlayerState.AddRage(time, spentRage * mods.rageKeep, false);
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            sendToSelf = false;
            rageCost = rage;
            failedToUse = false;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;

            if (value > AbilityFunctions.BladeWeaver.Max_Dash_Rage || value > rage)
            {
                player.client.SendAsync(new TitanCore.Net.Packets.Server.TnError("Invalid rage use amount!"));
                failedToUse = true;
                return null;
            }

            rage -= value;
            spentRage = value;
            slashHit = false;
            uint duration = AbilityFunctions.BladeWeaver.Dash_Duration + (uint)Math.Max(0, mods.durationBonusMs);
            dashEndTime = time + duration;
            waitingInvuln = true;
            PlayerState.AddDashing(position, target, time, value, duration, mods.abilityRangeBonus);

            var worldEffectPacket = PlayColored(new BladeweaverAbilityWorldEffect(player.gameId, duration));

            var bladeweaverItem = new Item(0x2a8);
            var bladeweaverWeaponInfo = (WeaponInfo)bladeweaverItem.GetInfo();
            var bladeweaverProjData = bladeweaverWeaponInfo.projectiles[0];

            int weaponDamage = player.GetHeldWeaponVolleyDamage(player.projIds, time);
            var bladeweaverProjectiles = player.GetProjectiles(bladeweaverItem, bladeweaverProjData, bladeweaverWeaponInfo, player.projIds, player.gameId, position.AngleTo(target), false, time);
            int damage = AbilityFunctions.BladeWeaver.ScaleWeaponDamage(weaponDamage, value);
            damage = (int)(damage * (1f + mods.abilityDamagePct));
            var bwProjDamage = (ushort)Math.Max(1, damage);
            for (int i = 0; i < bladeweaverProjectiles.Length; i++)
                bladeweaverProjectiles[i].damage = bwProjDamage;

            player.gameState.AddPlayerProjectiles(time, position, bladeweaverProjectiles);
            foreach (var otherPlayer in player.playersSentTo)
                if (player != otherPlayer)
                    otherPlayer.gameState.AddAllyProjectiles(bladeweaverProjectiles);

            return worldEffectPacket;
        }
    }
}
