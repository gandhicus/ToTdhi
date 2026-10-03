using System;
using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using Utils.NET.IO.Xml;
using Utils.NET.Utils;

namespace TitanCore.Data.Components
{
    public class StatRollPool
    {
        public int rollCount = 3;

        public int maxPerStat = 2;

        public int skipChance = 0;

        /// <summary>
        /// Normalized expected amount for roll values: 0 is the minimum, 1 is the maximum,
        /// and 0.5 preserves a uniform roll. Individual entries may override this value.
        /// </summary>
        public float amountAverage = 0.5f;

        public List<StatRollEntry> entries = new List<StatRollEntry>();

        public StatRollPool(XmlParser xml)
        {
            rollCount = xml.AtrInt("rollCount", 3);
            maxPerStat = xml.AtrInt("maxPerStat", 2);
            skipChance = xml.AtrInt("skipChance", 0);
            amountAverage = Math.Max(0f, Math.Min(1f, xml.AtrFloat("amountAverage", 0.5f)));

            foreach (var element in xml.Elements("StatRoll"))
                entries.Add(new StatRollEntry(element, false));

            foreach (var element in xml.Elements("AlternateStatRoll"))
                entries.Add(new StatRollEntry(element, true));
        }

        public void ApplyRolls(ref Item item)
        {
            var rolledStats = new Dictionary<StatType, int>();
            var rolledAltStats = new Dictionary<AlternateStatType, int>();
            var pickCounts = new Dictionary<string, int>();

            // Inherent ±2 (always) is applied first and never skipped, so old items without
            // rolls keep exact StatIncrease while new drops always get variance stored.
            foreach (var alwaysEntry in entries.Where(e => e.always))
            {
                var amount = RollAmount(alwaysEntry);
                AddRolledAmount(alwaysEntry, amount, rolledStats, rolledAltStats);
            }

            for (int i = 0; i < rollCount; i++)
            {
                // skipChance is a percent (0–100); a skipped attempt is consumed, not rerolled.
                if (skipChance > 0 && Rand.Next(100) < skipChance)
                    continue;

                var available = entries.Where(e =>
                    !e.always &&
                    e.weight > 0 &&
                    (!pickCounts.TryGetValue(e.GetKey(), out var count) || count < maxPerStat)
                ).ToList();

                if (available.Count == 0)
                    break;

                var picked = PickWeighted(available);
                if (picked == null)
                    break;

                var amount = RollAmount(picked);
                var key = picked.GetKey();
                pickCounts[key] = pickCounts.TryGetValue(key, out var existing) ? existing + 1 : 1;
                AddRolledAmount(picked, amount, rolledStats, rolledAltStats);
            }

            item.rolledStatIncreases = rolledStats;
            item.rolledAlternateStatIncreases = rolledAltStats;
        }

        public bool TryGetPrimaryDisplayRange(StatType type, IReadOnlyDictionary<StatType, int> fixedStats, out int min, out int max)
        {
            min = 0;
            max = 0;
            var alwaysEntry = entries.FirstOrDefault(e => e.always && !e.isAlternate && e.statType == type);
            if (alwaysEntry != null)
            {
                int baseline = 0;
                if (fixedStats != null)
                    fixedStats.TryGetValue(type, out baseline);
                min = baseline + alwaysEntry.min;
                max = baseline + alwaysEntry.max;
                return true;
            }

            if (maxPerStat > 1)
                return false;

            var bonus = entries.FirstOrDefault(e => !e.always && !e.isAlternate && e.statType == type);
            if (bonus == null)
                return false;

            min = bonus.min;
            max = bonus.max;
            return true;
        }

        public bool TryGetPrimaryRollRange(StatType type, out int min, out int max)
        {
            min = 0;
            max = 0;
            var alwaysEntry = entries.FirstOrDefault(e => e.always && !e.isAlternate && e.statType == type);
            if (alwaysEntry != null)
            {
                min = alwaysEntry.min;
                max = alwaysEntry.max;
                return true;
            }

            if (maxPerStat > 1)
                return false;

            var bonus = entries.FirstOrDefault(e => !e.always && !e.isAlternate && e.statType == type);
            if (bonus == null)
                return false;

            min = bonus.min;
            max = bonus.max;
            return true;
        }

        public bool TryGetAlternateDisplayRange(AlternateStatType type, IReadOnlyDictionary<AlternateStatType, int> fixedStats, out int min, out int max)
        {
            min = 0;
            max = 0;
            var alwaysEntry = entries.FirstOrDefault(e => e.always && e.isAlternate && e.alternateStatType == type);
            if (alwaysEntry != null)
            {
                int baseline = 0;
                if (fixedStats != null)
                    fixedStats.TryGetValue(type, out baseline);
                min = baseline + alwaysEntry.min;
                max = baseline + alwaysEntry.max;
                return true;
            }

            if (maxPerStat > 1)
                return false;

            var bonus = entries.FirstOrDefault(e => !e.always && e.isAlternate && e.alternateStatType == type);
            if (bonus == null)
                return false;

            min = bonus.min;
            max = bonus.max;
            return true;
        }

        public bool TryGetAlternateRollRange(AlternateStatType type, out int min, out int max)
        {
            min = 0;
            max = 0;
            var alwaysEntry = entries.FirstOrDefault(e => e.always && e.isAlternate && e.alternateStatType == type);
            if (alwaysEntry != null)
            {
                min = alwaysEntry.min;
                max = alwaysEntry.max;
                return true;
            }

            if (maxPerStat > 1)
                return false;

            var bonus = entries.FirstOrDefault(e => !e.always && e.isAlternate && e.alternateStatType == type);
            if (bonus == null)
                return false;

            min = bonus.min;
            max = bonus.max;
            return true;
        }

        private static void AddRolledAmount(
            StatRollEntry picked,
            int amount,
            Dictionary<StatType, int> rolledStats,
            Dictionary<AlternateStatType, int> rolledAltStats)
        {
            if (picked.isAlternate)
            {
                if (!rolledAltStats.TryGetValue(picked.alternateStatType, out var current))
                    current = 0;
                rolledAltStats[picked.alternateStatType] = current + amount;
            }
            else
            {
                if (!rolledStats.TryGetValue(picked.statType, out var current))
                    current = 0;
                rolledStats[picked.statType] = current + amount;
            }
        }

        private static StatRollEntry PickWeighted(List<StatRollEntry> available)
        {
            int total = 0;
            for (int i = 0; i < available.Count; i++)
                total += available[i].weight;

            if (total <= 0)
                return null;

            int roll = Rand.Next(total);
            for (int i = 0; i < available.Count; i++)
            {
                roll -= available[i].weight;
                if (roll < 0)
                    return available[i];
            }

            return available[available.Count - 1];
        }

        private int RollAmount(StatRollEntry entry)
        {
            int count = entry.max - entry.min + 1;
            if (count <= 1)
                return entry.min;

            float average = entry.amountAverage >= 0f ? entry.amountAverage : amountAverage;
            average = Math.Max(0f, Math.Min(1f, average));
            if (average <= 0f)
                return entry.min;
            if (average >= 1f)
                return entry.max;
            if (Math.Abs(average - 0.5f) < 0.0001f)
                return Rand.Range(entry.min, entry.max + 1);

            // Choose exponentially weighted outcomes. A binary search finds the curve whose
            // discrete average matches the XML value, so lower outcomes are more common when
            // amountAverage is below 0.5 without changing the stated min/max range.
            double low = -32d;
            double high = 32d;
            for (int iteration = 0; iteration < 24; iteration++)
            {
                double curve = (low + high) * 0.5d;
                if (GetNormalizedAverage(count, curve) < average)
                    low = curve;
                else
                    high = curve;
            }

            double selectedCurve = (low + high) * 0.5d;
            double totalWeight = 0d;
            for (int i = 0; i < count; i++)
                totalWeight += Math.Exp(selectedCurve * i / (count - 1d));

            double roll = Rand.FloatValue() * totalWeight;
            for (int i = 0; i < count; i++)
            {
                roll -= Math.Exp(selectedCurve * i / (count - 1d));
                if (roll < 0d)
                    return entry.min + i;
            }

            return entry.max;
        }

        private static double GetNormalizedAverage(int count, double curve)
        {
            double totalWeight = 0d;
            double weightedValue = 0d;
            for (int i = 0; i < count; i++)
            {
                double weight = Math.Exp(curve * i / (count - 1d));
                totalWeight += weight;
                weightedValue += weight * i / (count - 1d);
            }
            return weightedValue / totalWeight;
        }
    }
}
