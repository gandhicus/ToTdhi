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

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnMove(Vec2 position, uint time)
        {
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
            uint rangeDuration = (uint)(2500 + 11000 * rageScalar * attackScalar) + (uint)Math.Max(0, mods.durationBonusMs);
            float rangeArea = 2.5f + 6f * rageScalar + Math.Max(0f, mods.abilityRangeBonus);
            int defenseBonus = AbilityFunctions.Commander.GetDefenseBonus(attack, spent);

            PlayerState.ApplyCommanderFieldStatBonus(StatType.Defense, defenseBonus, time, defDuration);

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

            return PlayColored(new CommanderAbilityWorldEffect(player.gameId, position, spent, attack));
        }
    }
}
