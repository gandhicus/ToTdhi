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

        public List<StatRollEntry> entries = new List<StatRollEntry>();

        public StatRollPool(XmlParser xml)
        {
            rollCount = xml.AtrInt("rollCount", 3);
            maxPerStat = xml.AtrInt("maxPerStat", 2);

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

            for (int i = 0; i < rollCount; i++)
            {
                var available = entries.Where(e =>
                    !pickCounts.TryGetValue(e.GetKey(), out var count) || count < maxPerStat
                ).ToList();

                if (available.Count == 0)
                    break;

                var picked = available[Rand.Next(available.Count)];
                var amount = Rand.Range(picked.min, picked.max + 1);

                var key = picked.GetKey();
                pickCounts[key] = pickCounts.TryGetValue(key, out var existing) ? existing + 1 : 1;

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

            item.rolledStatIncreases = rolledStats;
            item.rolledAlternateStatIncreases = rolledAltStats;
        }
    }
}
