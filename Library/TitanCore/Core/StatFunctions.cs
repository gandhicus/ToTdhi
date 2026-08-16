using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data.Entities;
using TitanCore.Net;

namespace TitanCore.Core
{
    public static class StatFunctions
    {
        public static float TilesPerSecond(int speed, bool slowed, bool speedy)
        {
            var tps = 4f + (speed / 50.0f) * 4f;
            if (slowed)
                tps *= 0.5f;
            if (speedy)
                tps *= 1.5f;
            return tps;
        }

        public static float AttackModifier(int attack, bool damaging)
        {
            var modifier = 0.5f + (attack / 60.0f);
            if (damaging)
                modifier *= 1.5f;
            return modifier;
        }

        public static int DamageTaken(int defense, int damage, bool fortified)
        {
            if (damage <= 0)
                return 0;
            if (fortified)
                defense *= 2;
            int min = damage / 5;
            if (min == 0)
                min = 1;
            int taken = damage - defense;
            return taken < min ? min : taken;
        }

        public static uint GetCombatSeed(uint projectileId, uint time, uint targetId)
        {
            return projectileId ^ (time * 2654435761u) ^ targetId;
        }

        /// <summary>
        /// Seed for crit/true damage rolls shared by all projectiles that land on the same target in the same tick.
        /// </summary>
        public static uint GetLandingProcSeed(uint time, uint targetId, uint attackerId)
        {
            return (time * 2654435761u) ^ targetId ^ (attackerId * 2246822519u);
        }

        public static float CombatRoll(uint seed)
        {
            uint x = seed;
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }

        public static bool RollChance(int chancePercent, float roll)
        {
            if (chancePercent <= 0) return false;
            if (chancePercent >= 100) return true;
            return roll * 100f < chancePercent;
        }

        public static float CriticalStrikeMultiplier(int criticalStrikeDamageBonus)
        {
            return 1.5f + criticalStrikeDamageBonus * 0.01f;
        }

        public static DamageResult ResolveOutgoingDamage(
            int rawDamage,
            Item[] attackerEquips,
            int defenderBlockChance,
            int defenderAbsorptionChance,
            int defenderDefense,
            bool defenderFortified,
            uint projectileId,
            uint time,
            uint targetId,
            uint attackerId)
        {
            return ResolveOutgoingDamage(
                rawDamage,
                attackerEquips,
                0,
                0,
                0,
                defenderBlockChance,
                defenderAbsorptionChance,
                defenderDefense,
                defenderFortified,
                projectileId,
                time,
                targetId,
                attackerId);
        }

        public static DamageResult ResolveOutgoingDamage(
            int rawDamage,
            Item[] attackerEquips,
            int attackerTrueDamageBonus,
            int attackerCritChanceBonus,
            int attackerCritDamageBonus,
            int defenderBlockChance,
            int defenderAbsorptionChance,
            int defenderDefense,
            bool defenderFortified,
            uint projectileId,
            uint time,
            uint targetId,
            uint attackerId)
        {
            return ResolveDamage(
                rawDamage,
                ItemFunctions.GetEquippedAlternateStat(attackerEquips, AlternateStatType.TrueDamageChance) + attackerTrueDamageBonus,
                ItemFunctions.GetEquippedAlternateStat(attackerEquips, AlternateStatType.CriticalStrikeChance) + attackerCritChanceBonus,
                ItemFunctions.GetEquippedAlternateStat(attackerEquips, AlternateStatType.CriticalStrikeDamage) + attackerCritDamageBonus,
                defenderBlockChance,
                defenderAbsorptionChance,
                defenderDefense,
                defenderFortified,
                GetCombatSeed(projectileId, time, targetId),
                GetLandingProcSeed(time, targetId, attackerId));
        }

        public static DamageResult ResolveIncomingDamage(
            int rawDamage,
            Item[] defenderEquips,
            int attackerTrueDamageChance,
            int defenderDefense,
            bool defenderFortified,
            uint seed)
        {
            return ResolveIncomingDamage(
                rawDamage,
                defenderEquips,
                attackerTrueDamageChance,
                0,
                0,
                defenderDefense,
                defenderFortified,
                seed);
        }

        public static DamageResult ResolveIncomingDamage(
            int rawDamage,
            Item[] defenderEquips,
            int attackerTrueDamageChance,
            int defenderBlockBonus,
            int defenderAbsorptionBonus,
            int defenderDefense,
            bool defenderFortified,
            uint seed)
        {
            return ResolveDamage(
                rawDamage,
                attackerTrueDamageChance,
                0,
                0,
                ItemFunctions.GetEquippedAlternateStat(defenderEquips, AlternateStatType.BlockChance) + defenderBlockBonus,
                ItemFunctions.GetEquippedAlternateStat(defenderEquips, AlternateStatType.AbsorptionChance) + defenderAbsorptionBonus,
                defenderDefense,
                defenderFortified,
                seed,
                seed);
        }

        public static DamageResult ResolveDamage(
            int rawDamage,
            int trueDamageChance,
            int criticalStrikeChance,
            int criticalStrikeDamageBonus,
            int blockChance,
            int absorptionChance,
            int defense,
            bool fortified,
            uint hitSeed,
            uint procSeed)
        {
            if (RollChance(blockChance, CombatRoll(hitSeed)))
                return new DamageResult(0, HitResultType.Blocked);

            int damage = rawDamage;
            bool isCrit = RollChance(criticalStrikeChance, CombatRoll(procSeed + 2));
            if (isCrit)
                damage = (int)(damage * CriticalStrikeMultiplier(criticalStrikeDamageBonus));

            HitResultType type;
            int finalDamage;
            if (RollChance(trueDamageChance, CombatRoll(procSeed + 1)))
            {
                finalDamage = damage;
                type = HitResultType.TrueDamage;
            }
            else if (isCrit)
            {
                finalDamage = DamageTaken(defense, damage, fortified);
                type = HitResultType.Critical;
            }
            else
            {
                finalDamage = DamageTaken(defense, damage, fortified);
                type = HitResultType.Normal;
            }

            if (RollChance(absorptionChance, CombatRoll(hitSeed + 3)))
                return new DamageResult(-finalDamage, HitResultType.Absorbed, isCrit);

            return new DamageResult(finalDamage, type, isCrit);
        }

        public static float HealthRegen(int vigor, int timeMs, bool healing, bool sick)
        {
            if (sick) return 0;
            float healingPerSecond = (2 + (vigor / 50.0f) * 10);
            if (healing)
                healingPerSecond += 20;
            return healingPerSecond * (timeMs / 1000f);
        }

        public static int GetLevelUpCost(CharacterInfo info, StatType type, int currentStat, int change)
        {
            int cost = 0;
            int max = info.stats[type].maxValue;
            for (int i = currentStat; i < currentStat + change; i++)
            {
                if (i < max)
                    cost += i * 4;
                else
                {
                    float toPower = (i - (max - 4)) / 54f;
                    cost += (int)(1_000_000 * (toPower * toPower * toPower) + 600 - 6);
                }
            }
            if (type == StatType.MaxHealth)
                cost *= 2;
            return cost;
        }

        public static int GetAscensionCost(StatType type, int currentStat, int statLock, out Item itemCost)
        {
            itemCost = Item.Blank;
            if (statLock == 0) return -1;

            var ascensionIncrease = type == StatType.MaxHealth ? 10 : 1;
            var ascensionCount = (currentStat - statLock) / ascensionIncrease;
            if (ascensionCount >= NetConstants.Max_Ascension) return -1;

            var nextAscension = ascensionCount + 1;

            switch (type)
            {
                case StatType.MaxHealth:
                    itemCost = new Item(0x2ae);
                    break;
                case StatType.Speed:
                    itemCost = new Item(0x2a2);
                    break;
                case StatType.Attack:
                    itemCost = new Item(0x2a3);
                    break;
                case StatType.Defense:
                    itemCost = new Item(0x2a4);
                    break;
                case StatType.Vigor:
                    itemCost = new Item(0x2a5);
                    break;
            }

            itemCost.count = (byte)(((nextAscension - 1) / NetConstants.Ascension_Increases_Per_Scroll_Cost) + 1);

            var soulCost = (double)NetConstants.Ascension_Base_Soul_Cost;
            for (int ascension = 2; ascension <= nextAscension; ascension++)
            {
                var decay = 1 + NetConstants.Ascension_Soul_Cost_Decay * (ascension - 2);
                var increase = 1 + NetConstants.Ascension_Base_Soul_Cost_Increase / decay;
                soulCost *= increase;
            }

            return (int)Math.Ceiling(soulCost);
        }

        public static float AttackSpeedModifier(bool fervent, int rofIncreases)
        {
            float rof = 1;
            rof += rofIncreases * 0.01f;
            if (fervent)
                rof *= 1.5f;
            return rof;
        }

        public static float ApplyRageGainBonus(float baseAmount, int rageGainBonus)
        {
            return baseAmount * (1 + rageGainBonus * 0.01f);
        }

        public static uint ApplyResistanceDuration(uint duration, int resistancePercent)
        {
            if (resistancePercent <= 0) return duration;
            if (resistancePercent >= 100) return 0;
            return (uint)(duration * (1 - resistancePercent * 0.01f));
        }

        public static float ApplyResistanceMultiplier(int resistancePercent)
        {
            if (resistancePercent <= 0) return 1f;
            if (resistancePercent >= 100) return 0f;
            return 1 - resistancePercent * 0.01f;
        }

        public static float ApplyAbilityRageSpend(float rageBefore, byte rageIntegralBefore, byte rageIntegralAfter)
        {
            var integralSpent = rageIntegralBefore - rageIntegralAfter;
            if (rageIntegralAfter == 0 && integralSpent > 0)
                return 0f;

            return Math.Min(100f, rageBefore - integralSpent);
        }
    }
}
