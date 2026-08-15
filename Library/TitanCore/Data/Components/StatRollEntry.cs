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

        public StatRollEntry(XmlParser xml, bool isAlternate)
        {
            this.isAlternate = isAlternate;
            if (isAlternate)
                alternateStatType = xml.AtrEnum("type", AlternateStatType.RateOfFire);
            else
                statType = xml.AtrEnum("type", StatType.Speed);

            min = xml.AtrInt("min", 1);
            max = xml.AtrInt("max", min);
        }

        public string GetKey()
        {
            return isAlternate ? "A:" + alternateStatType : "S:" + statType;
        }
    }
}
