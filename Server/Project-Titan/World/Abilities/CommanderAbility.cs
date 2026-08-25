using System;
using TitanCore.Core;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;

namespace World.Abilities
{
    public class CommanderAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Commander;

        private uint defenseEndTime;
        private uint nextPulse;
        private int pulseDefense;
        private uint pulseLockout;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override void Tick(uint time)
        {
            if (time > defenseEndTime) return;
            if (time < nextPulse) return;
            nextPulse = time + pulseLockout;
            ApplyPulseDefense(time);
            TriggerTalisman(TalismanTrigger.AbilityTick, time, player.position.Value, player.position.Value);
            if (time + pulseLockout > defenseEndTime)
                TriggerTalisman(TalismanTrigger.AbilityEnd, time, player.position.Value, player.position.Value);
        }

        private void ApplyPulseDefense(uint time)
        {
            if (pulseDefense <= 0 || time >= defenseEndTime) return;
            var remaining = defenseEndTime - time;
            PlayerState.ApplyCommanderPulseStatBonus(StatType.Defense, pulseDefense, time, remaining);
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            sendToSelf = false;
            failedToUse = false;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            byte spent = rage;
            SpendDumpRage(ref rage, mods, out rageCost);

            float rageScalar = spent / 100f;
            float attackScalar = 0.5f + attack / 50f;
            uint defDuration = (uint)((1000 + 7000 * rageScalar * attackScalar) * (mods.durationMul > 0 ? mods.durationMul : 1f));
            uint rangeDuration = (uint)(2500 + 11000 * rageScalar * attackScalar) + (uint)Math.Max(0, mods.durationBonusMs);
            float rangeArea = 2.5f + 6f * rageScalar + mods.abilityRangeBonus;
            int defenseAmt = (int)(25 + 50 * rageScalar) + mods.hymnDefense;

            PlayerState.ApplyCommanderFieldStatBonus(StatType.Defense, defenseAmt, time, defDuration);
            if (mods.hymnMaxHealth > 0)
                PlayerState.ApplyCommanderFieldStatBonus(StatType.MaxHealth, mods.hymnMaxHealth, time, defDuration);

            foreach (var other in player.world.objects.GetPlayersWithin(position.x, position.y, rangeArea))
                other.gameState.playerState.AddClientStatusEffect(StatusEffect.Reach, time, rangeDuration);

            defenseEndTime = time + defDuration;
            pulseLockout = (uint)Math.Max(200, mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : 500);
            pulseDefense = Math.Max(1, defenseAmt / 4);
            ApplyPulseDefense(time);
            TriggerTalisman(TalismanTrigger.AbilityTick, time, position, position);
            nextPulse = time + pulseLockout;

            return PlayColored(new CommanderAbilityWorldEffect(player.gameId, position, spent, attack));
        }
    }
}
