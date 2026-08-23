using System.Collections.Generic;
using TitanCore.Data.Components;
using TitanCore.Data.Items;

namespace TitanCore.Core
{
    public static class EquipmentStatFunctions
    {
        public static int ComputeScaledAmount(int sourceTotal, int perAmount)
        {
            if (perAmount <= 0 || sourceTotal <= 0) return 0;
            return sourceTotal / perAmount;
        }

        public static Dictionary<StatType, int> GetFixedStatIncreases(Item[] equips)
        {
            var stats = new Dictionary<StatType, int>();
            if (equips == null) return stats;

            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].IsBlank) continue;
                if (!(equips[i].GetInfo() is EquipmentInfo equip)) continue;
                foreach (var increase in GetStatIncreases(equips[i], equip))
                {
                    if (!stats.TryGetValue(increase.Key, out var amount))
                        amount = 0;
                    stats[increase.Key] = amount + increase.Value;
                }
            }

            return stats;
        }

        public static Dictionary<AlternateStatType, int> GetFixedAlternateStatIncreases(Item[] equips)
        {
            var stats = new Dictionary<AlternateStatType, int>();
            if (equips == null) return stats;

            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].IsBlank) continue;
                if (!(equips[i].GetInfo() is EquipmentInfo equip)) continue;
                foreach (var increase in GetAlternateStatIncreases(equips[i], equip))
                {
                    if (!stats.TryGetValue(increase.Key, out var amount))
                        amount = 0;
                    stats[increase.Key] = amount + increase.Value;
                }
            }

            return stats;
        }

        public static int GetScaledStatAmount(
            ScaledStatIncrease scaled,
            IReadOnlyDictionary<StatType, int> fixedStats,
            IReadOnlyDictionary<AlternateStatType, int> fixedAlternateStats = null)
        {
            int sourceAmount = 0;
            if (scaled.fromIsAlternate)
            {
                if (fixedAlternateStats != null)
                    fixedAlternateStats.TryGetValue(scaled.fromAlternateStat, out sourceAmount);
            }
            else if (fixedStats != null)
                fixedStats.TryGetValue(scaled.fromStat, out sourceAmount);

            return ComputeScaledAmount(sourceAmount, scaled.perAmount) * scaled.gainAmount;
        }

        public static Dictionary<StatType, int> GetDisplayStatIncreases(
            Item item,
            EquipmentInfo equip,
            IReadOnlyDictionary<StatType, int> equippedFixedStats,
            bool includeItemFixedStats,
            IReadOnlyDictionary<AlternateStatType, int> equippedFixedAlternateStats = null,
            bool includeItemFixedAlternateStats = true)
        {
            var stats = new Dictionary<StatType, int>();
            foreach (var increase in GetStatIncreases(item, equip))
            {
                if (increase.Value == 0) continue;
                stats[increase.Key] = increase.Value;
            }

            var fixedStats = equippedFixedStats == null
                ? new Dictionary<StatType, int>()
                : CopyStats(equippedFixedStats);

            if (includeItemFixedStats)
            {
                foreach (var increase in GetStatIncreases(item, equip))
                {
                    if (!fixedStats.TryGetValue(increase.Key, out var amount))
                        amount = 0;
                    fixedStats[increase.Key] = amount + increase.Value;
                }
            }

            var fixedAlternateStats = equippedFixedAlternateStats == null
                ? new Dictionary<AlternateStatType, int>()
                : CopyAlternateStats(equippedFixedAlternateStats);

            if (includeItemFixedAlternateStats)
            {
                foreach (var increase in GetAlternateStatIncreases(item, equip))
                {
                    if (!fixedAlternateStats.TryGetValue(increase.Key, out var amount))
                        amount = 0;
                    fixedAlternateStats[increase.Key] = amount + increase.Value;
                }
            }

            var alternateStats = new Dictionary<AlternateStatType, int>();
            foreach (var scaled in equip.scaledStatIncreases)
                ApplyScaledStatIncrease(scaled, fixedStats, stats, alternateStats, fixedAlternateStats);

            return stats;
        }

        public static void RecalculateEquipmentStats(
            Item[] equips,
            Dictionary<StatType, int> stats,
            Dictionary<AlternateStatType, int> alternateStats)
        {
            stats.Clear();
            alternateStats.Clear();

            var fixedStats = GetFixedStatIncreases(equips);
            var fixedAlternateStats = GetFixedAlternateStatIncreases(equips);
            foreach (var increase in fixedStats)
                stats[increase.Key] = increase.Value;

            if (equips == null) return;

            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].IsBlank) continue;
                if (!(equips[i].GetInfo() is EquipmentInfo equip)) continue;
                AddAlternateStatIncreases(equips[i], equip, alternateStats);

                foreach (var scaled in equip.scaledStatIncreases)
                    ApplyScaledStatIncrease(scaled, fixedStats, stats, alternateStats, fixedAlternateStats);
            }
        }

        private static void ApplyScaledStatIncrease(
            ScaledStatIncrease scaled,
            IReadOnlyDictionary<StatType, int> fixedStats,
            Dictionary<StatType, int> stats,
            Dictionary<AlternateStatType, int> alternateStats,
            IReadOnlyDictionary<AlternateStatType, int> fixedAlternateStats = null)
        {
            var amount = GetScaledStatAmount(scaled, fixedStats, fixedAlternateStats);
            if (amount == 0) return;

            if (scaled.toIsAlternate)
            {
                if (alternateStats == null) return;
                if (!alternateStats.TryGetValue(scaled.toAlternateStat, out var current))
                    current = 0;
                alternateStats[scaled.toAlternateStat] = current + amount;
                return;
            }

            if (stats == null) return;
            if (!stats.TryGetValue(scaled.toStat, out var statCurrent))
                statCurrent = 0;
            stats[scaled.toStat] = statCurrent + amount;
        }

        public static Dictionary<StatType, int> CopyStatsForDisplay(IReadOnlyDictionary<StatType, int> stats)
        {
            return CopyStats(stats);
        }

        public static Dictionary<AlternateStatType, int> CopyAlternateStatsForDisplay(IReadOnlyDictionary<AlternateStatType, int> stats)
        {
            return CopyAlternateStats(stats);
        }

        private static Dictionary<StatType, int> CopyStats(IReadOnlyDictionary<StatType, int> stats)
        {
            var copy = new Dictionary<StatType, int>();
            foreach (var increase in stats)
                copy[increase.Key] = increase.Value;
            return copy;
        }

        private static Dictionary<AlternateStatType, int> CopyAlternateStats(IReadOnlyDictionary<AlternateStatType, int> stats)
        {
            var copy = new Dictionary<AlternateStatType, int>();
            foreach (var increase in stats)
                copy[increase.Key] = increase.Value;
            return copy;
        }

        public static void GetTooltipScalingContext(
            Item[] equips,
            Item item,
            out IReadOnlyDictionary<StatType, int> equippedFixedStats,
            out IReadOnlyDictionary<AlternateStatType, int> equippedFixedAlternateStats,
            out bool includeItemForScaling)
        {
            equippedFixedStats = GetFixedStatIncreases(equips);
            equippedFixedAlternateStats = GetFixedAlternateStatIncreases(equips);
            includeItemForScaling = true;
            if (equips == null) return;

            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].Equals(item))
                {
                    includeItemForScaling = false;
                    break;
                }
            }
        }

        public static bool HasRolledStats(Item item)
        {
            return (item.rolledStatIncreases != null && item.rolledStatIncreases.Count > 0)
                || (item.rolledAlternateStatIncreases != null && item.rolledAlternateStatIncreases.Count > 0);
        }

        public static IReadOnlyDictionary<StatType, int> GetStatIncreases(Item item, EquipmentInfo equip)
        {
            if (!HasRolledStats(item))
                return equip.statIncreases;

            return MergeStatIncreases(equip.statIncreases, item.rolledStatIncreases);
        }

        public static IReadOnlyDictionary<AlternateStatType, int> GetAlternateStatIncreases(Item item, EquipmentInfo equip)
        {
            if (!HasRolledStats(item))
                return equip.alternateStatIncreases;

            return MergeAlternateStatIncreases(equip.alternateStatIncreases, item.rolledAlternateStatIncreases);
        }

        private static Dictionary<StatType, int> MergeStatIncreases(
            IReadOnlyDictionary<StatType, int> fixedStats,
            Dictionary<StatType, int> rolledStats)
        {
            var merged = new Dictionary<StatType, int>();
            foreach (var increase in fixedStats)
                merged[increase.Key] = increase.Value;

            if (rolledStats == null) return merged;

            foreach (var increase in rolledStats)
            {
                if (!merged.TryGetValue(increase.Key, out var amount))
                    amount = 0;
                merged[increase.Key] = amount + increase.Value;
            }

            return merged;
        }

        private static Dictionary<AlternateStatType, int> MergeAlternateStatIncreases(
            IReadOnlyDictionary<AlternateStatType, int> fixedStats,
            Dictionary<AlternateStatType, int> rolledStats)
        {
            var merged = new Dictionary<AlternateStatType, int>();
            foreach (var increase in fixedStats)
                merged[increase.Key] = increase.Value;

            if (rolledStats == null) return merged;

            foreach (var increase in rolledStats)
            {
                if (!merged.TryGetValue(increase.Key, out var amount))
                    amount = 0;
                merged[increase.Key] = amount + increase.Value;
            }

            return merged;
        }

        public static void AddStatIncreases(Item item, EquipmentInfo equip, Dictionary<StatType, int> stats)
        {
            foreach (var increase in GetStatIncreases(item, equip))
            {
                if (!stats.TryGetValue(increase.Key, out var amount))
                    amount = 0;
                stats[increase.Key] = amount + increase.Value;
            }
        }

        public static void RemoveStatIncreases(Item item, EquipmentInfo equip, Dictionary<StatType, int> stats)
        {
            foreach (var increase in GetStatIncreases(item, equip))
            {
                if (!stats.TryGetValue(increase.Key, out var amount))
                    continue;
                amount -= increase.Value;
                if (amount == 0)
                    stats.Remove(increase.Key);
                else
                    stats[increase.Key] = amount;
            }
        }

        public static void AddAlternateStatIncreases(Item item, EquipmentInfo equip, Dictionary<AlternateStatType, int> stats)
        {
            foreach (var increase in GetAlternateStatIncreases(item, equip))
            {
                if (!stats.TryGetValue(increase.Key, out var amount))
                    amount = 0;
                stats[increase.Key] = amount + increase.Value;
            }
        }

        public static void RemoveAlternateStatIncreases(Item item, EquipmentInfo equip, Dictionary<AlternateStatType, int> stats)
        {
            foreach (var increase in GetAlternateStatIncreases(item, equip))
            {
                if (!stats.TryGetValue(increase.Key, out var amount))
                    continue;
                amount -= increase.Value;
                if (amount == 0)
                    stats.Remove(increase.Key);
                else
                    stats[increase.Key] = amount;
            }
        }

        public static bool RolledStatsEqual(Item a, Item b)
        {
            return DictionaryEquals(a.rolledStatIncreases, b.rolledStatIncreases)
                && DictionaryEquals(a.rolledAlternateStatIncreases, b.rolledAlternateStatIncreases);
        }

        private static bool DictionaryEquals<TKey>(Dictionary<TKey, int> a, Dictionary<TKey, int> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                    return false;
            }
            return true;
        }
    }
}
