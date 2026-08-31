using System;
using TitanCore.Core;
using TitanCore.Data.Components.Projectiles;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;
using World.Map.Objects.Entities;

namespace World.Abilities
{
    public class WarriorAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Warrior;

        public uint abilityEndTime;

        public int abilityHealAmount;

        public uint nextHealTime = 0;

        private bool active = false;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
            if (time < nextHealTime || time > abilityEndTime) return;

            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            int lockout = mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : SkillTreeFunctions.Base_Pulse_Lockout_Ms;
            nextHealTime = time + (uint)lockout;

            var position = player.position.Value;
            var pulse = new WarriorAbilityWorldEffect(player.gameId);
            ColorWorldEffect(pulse);
            var effect = new TnPlayEffect(pulse);

            foreach (var otherPlayer in player.world.objects.GetPlayersWithin(position.x, position.y, AbilityFunctions.Warrior.Heal_Area))
            {
                if (otherPlayer == player) continue;
                otherPlayer.Heal(abilityHealAmount);
                otherPlayer.client.SendAsync(effect);
            }
            player.Heal(abilityHealAmount);
            player.client.SendAsync(effect);

            TriggerTalisman(TalismanTrigger.AbilityPulse, time, position, entity.GetPosition(time));

            if (SkillTreeFunctions.IsEnabled && mods.weaponDamagePct > 0)
                damageTaken += GetCleaveDamageTaken(entity, time, mods);
        }

        public override void OnMove(Vec2 position, uint time)
        {
            if (time > abilityEndTime && active)
            {
                active = false;
                player.SetSize(1);
            }
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            active = true;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;

            byte keep = 0;
            if (SkillTreeFunctions.IsEnabled && mods.rageKeep > 0)
                keep = (byte)Math.Round(rage * mods.rageKeep);

            rageCost = (byte)Math.Max(0, rage - keep);
            if (rageCost == 0 && rage > 0)
                rageCost = rage;

            uint duration = AbilityFunctions.Warrior.GetAbilityDuration(rageCost);
            if (SkillTreeFunctions.IsEnabled)
                duration += (uint)Math.Max(0, mods.durationBonusMs);
            abilityEndTime = time + duration;

            int heal = AbilityFunctions.Warrior.GetHealAmount(rageCost, attack);
            if (SkillTreeFunctions.IsEnabled && mods.healPower != 0)
                heal = (int)(heal * (1f + mods.healPower));
            abilityHealAmount = Math.Max(0, heal);

            player.SetSize(1.2f);

            if (SkillTreeFunctions.IsEnabled)
            {
                SkillTreeFunctions.ApplyRageToOnUseStats(ref mods, rage);
                if (mods.hymnDefense > 0)
                    PlayerState.ApplyTimedStatBonus(StatType.Defense, mods.hymnDefense, time, (uint)mods.hymnDefenseMs);
                if (mods.hymnMaxHealth > 0)
                    PlayerState.ApplyTimedStatBonus(StatType.MaxHealth, mods.hymnMaxHealth, time, (uint)mods.hymnMaxHealthMs);
                TriggerTalisman(TalismanTrigger.AbilityPulse, time, position, target);
            }

            sendToSelf = false;
            rage = keep;
            failedToUse = false;
            return null;
        }

        private int GetCleaveDamageTaken(EntityState entity, uint time, AbilityModifierSnapshot mods)
        {
            var weapon = PlayerState.currentSnapshot.equips != null && PlayerState.currentSnapshot.equips.Length > 0
                ? PlayerState.currentSnapshot.equips[0]
                : Item.Blank;
            if (weapon.IsBlank || !(weapon.GetInfo() is WeaponInfo weaponInfo) || weaponInfo.projectiles == null || weaponInfo.projectiles.Length == 0)
                return 0;

            ProjectileData proj = weaponInfo.projectiles[0];
            WeaponFunctions.GetProjectileDamage(weaponInfo.slotType, proj, out var minDamage, out var maxDamage);
            float mid = (minDamage + maxDamage) * 0.5f;
            int attack = PlayerState.currentSnapshot.GetFunctionalStat(StatType.Attack);
            bool damaging = PlayerState.HasEffect(StatusEffect.Damaging, time);
            float outgoing = mid * StatFunctions.AttackModifier(attack, damaging) * mods.weaponDamagePct;
            if (outgoing <= 0) return 0;

            return entity.GetDamageTaken((int)outgoing);
        }
    }
}
