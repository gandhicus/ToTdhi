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
        private uint pulseLockout;
        private int pulseStacks;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
            if (time > defenseEndTime) return;
            if (time < nextPulse) return;
            if (pulseStacks >= AbilityFunctions.Commander.MaxPulseStacks) return;

            nextPulse = time + pulseLockout;
            pulseStacks++;
            ApplyPulseDefense(time);
            TriggerTalisman(TalismanTrigger.AbilityPulse, time, player.position.Value, entity.GetPosition(time));
            if (pulseStacks >= AbilityFunctions.Commander.MaxPulseStacks || time + pulseLockout > defenseEndTime)
                TriggerTalisman(TalismanTrigger.AbilityEnd, time, player.position.Value, player.position.Value);
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        private void ApplyPulseDefense(uint time)
        {
            if (time >= defenseEndTime) return;
            var remaining = defenseEndTime - time;
            PlayerState.ApplyCommanderPulseStatBonus(StatType.Defense, AbilityFunctions.Commander.PulseDefense, time, remaining);
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
            uint defDuration = (uint)(AbilityFunctions.Commander.GetDefenseDurationMs(spent, attack) * (mods.durationMul > 0 ? mods.durationMul : 1f));
            uint rangeBase = (uint)(2500 + 11000 * rageScalar * attackScalar);
            float unfurlMul = 1f + Math.Max(0f, mods.abilityRangeBonus);
            uint rangeDuration = (uint)(rangeBase * unfurlMul) + (uint)Math.Max(0, mods.durationBonusMs);
            float rangeArea = 2.5f + 6f * rageScalar;

            SkillTreeFunctions.ApplyRageToOnUseStats(ref mods, spent);
            if (mods.hymnDefense > 0)
                PlayerState.ApplyCommanderFieldStatBonus(StatType.Defense, mods.hymnDefense, time, (uint)mods.hymnDefenseMs);
            if (mods.timedAttack > 0)
                PlayerState.ApplyCommanderFieldStatBonus(StatType.Attack, mods.timedAttack, time, (uint)mods.timedAttackMs);
            if (mods.hymnBlockChance > 0)
                PlayerState.ApplyTimedAlternateStatBonus(AlternateStatType.BlockChance, mods.hymnBlockChance, time, (uint)mods.hymnBlockChanceMs);

            float reachSec = rangeDuration / 1000f;
            PlayerState.AddClientStatusEffect(StatusEffect.Reach, time, rangeDuration);
            player.AddEffect(StatusEffect.Reach, reachSec);
            foreach (var other in player.world.objects.GetPlayersWithin(position.x, position.y, rangeArea))
            {
                if (other == player) continue;
                other.gameState.playerState.AddClientStatusEffect(StatusEffect.Reach, time, rangeDuration);
                other.AddEffect(StatusEffect.Reach, reachSec);
            }

            defenseEndTime = time + defDuration;
            pulseLockout = (uint)Math.Max(AbilityFunctions.Commander.MinPulseLockoutMs, mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : AbilityFunctions.Commander.BasePulseLockoutMs);
            nextPulse = 0;
            pulseStacks = 0;
            PlayerState.ClearCommanderPulseBonuses();

            return PlayColored(new CommanderAbilityWorldEffect(player.gameId, position, spent, attack));
        }
    }
}
