using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class ScaledStatIncrease
    {
        public int perAmount;

        public int gainAmount;

        public StatType fromStat;

        public StatType toStat;

        public ScaledStatIncrease(XmlParser xml)
        {
            perAmount = xml.AtrInt("per", 1);
            gainAmount = xml.AtrInt("amount", 1);
            fromStat = xml.AtrEnum("from", StatType.Defense);
            toStat = xml.AtrEnum("to", StatType.Vigor);
        }
    }
}
