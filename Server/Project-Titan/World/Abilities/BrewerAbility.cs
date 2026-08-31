using System;
using TitanCore.Core;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;

namespace World.Abilities
{
    public class BrewerAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Brewer;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            byte spent = rage;
            SpendDumpRage(ref rage, mods, out rageCost);
            SkillTreeFunctions.ApplyRageToOnUseStats(ref mods, spent);
            failedToUse = false;
            sendToSelf = false;

            float rageScalar = spent / 100f;
            float area = 6f + mods.abilityRadiusBonus;
            uint durationMs;
            if (value == 0)
                durationMs = 1000 + (uint)(10000 * rageScalar) + (uint)Math.Max(0, mods.durationBonusMs);
            else
                durationMs = 1000 + (uint)(8000 * rageScalar) + (uint)Math.Max(0, mods.durationBonusMs);

            foreach (var other in player.world.objects.GetPlayersWithin(position.x, position.y, area))
            {
                var state = other.gameState.playerState;
                if (value == 0)
                    state.ApplyTimedAlternateStatBonus(AlternateStatType.RateOfFire, AbilityFunctions.Brewer.RoF_Amount + mods.rofAmount, time, durationMs);
                else
                    state.ApplyTimedStatBonus(StatType.Vigor, 8 + mods.vigorBonus, time, durationMs);
            }

            if (mods.slowMs > 0)
            {
                foreach (var enemy in player.world.objects.GetEnemiesWithin(position.x, position.y, area))
                    enemy.AddEffect(StatusEffect.Slowed, mods.slowMs / 1000f);
            }

            if (mods.hymnDefense > 0 && mods.timedDefenseMs > 0)
                PlayerState.ApplyTimedStatBonus(StatType.Defense, mods.hymnDefense, time, (uint)mods.timedDefenseMs);

            return PlayColored(new BrewerAbilityWorldEffect(player.gameId, position, spent, attack, value));
        }
    }
}
