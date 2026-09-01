using System;
using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class StatRollEntry
    {
        public bool isAlternate;

        public StatType statType;

        public AlternateStatType alternateStatType;

        public int min;

        public int max;

        public int weight;

        public bool always;

        public StatRollEntry(XmlParser xml, bool isAlternate)
        {
            this.isAlternate = isAlternate;
            if (isAlternate)
                alternateStatType = xml.AtrEnum("type", AlternateStatType.RateOfFire);
            else
                statType = xml.AtrEnum("type", StatType.Speed);

            min = xml.AtrInt("min", 1);
            max = xml.AtrInt("max", min);
            weight = xml.AtrInt("weight", 1);
            always = string.Equals(xml.AtrString("always", "false"), "true", StringComparison.OrdinalIgnoreCase);
        }

        public string GetKey()
        {
            return isAlternate ? "A:" + alternateStatType : "S:" + statType;
        }
    }
}
