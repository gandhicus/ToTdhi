using System;
using TitanCore.Core;
using TitanCore.Data.Items;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;

namespace World.Abilities
{
    public class LancerAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Lancer;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            TriggerTalisman(TalismanTrigger.AbilityHit, time, player.position.Value, entity.GetPosition(time), ref damageTaken);
            if (mods.rageOnKill > 0 && player.world.objects.TryGetEnemy(entity.gameId, out var enemy) && enemy.GetHealth() - damageTaken <= 0)
                PlayerState.AddRage(time, mods.rageOnKill, false);
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            sendToSelf = false;
            failedToUse = false;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            int cost = (int)Math.Round(AbilityFunctions.Lancer.Rage_Cost - mods.rageCostFlat);
            cost = Math.Max(1, cost);
            rageCost = (byte)cost;

            var lancerItem = new Item(0x2a1);
            var lancerWeaponInfo = (WeaponInfo)lancerItem.GetInfo();
            var lancerProjData = lancerWeaponInfo.projectiles[0];

            float offset = AbilityFunctions.Lancer.GetAngleOffset(player.projIds);
            if (mods.wobbleMul > 0 && mods.wobbleMul != 1f)
                offset *= mods.wobbleMul;
            var projectiles = player.GetProjectiles(lancerItem, lancerProjData, lancerWeaponInfo, player.projIds, player.gameId, position.AngleTo(target) + offset, false, time);
            int damage = AbilityFunctions.Lancer.GetProjectileDamage(rage, attack);
            damage = (int)(damage * (1f + mods.abilityDamagePct));
            var proj = projectiles[0];
            proj.damage = (ushort)Math.Max(1, damage);
            projectiles[0] = proj;

            player.gameState.AddPlayerProjectiles(time, position, projectiles);
            foreach (var otherPlayer in player.playersSentTo)
                if (player != otherPlayer)
                    otherPlayer.gameState.AddAllyProjectiles(projectiles);

            if (mods.timedAttack > 0 && mods.timedAttackMs > 0)
                PlayerState.ApplyTimedStatBonus(StatType.Attack, mods.timedAttack, time, (uint)mods.timedAttackMs);

            rage = (byte)Math.Max(0, rage - cost);
            return null;
        }
    }
}
