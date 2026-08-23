using System;
using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class ScaledStatIncrease
    {
        public int perAmount;

        public int gainAmount;

        public bool fromIsAlternate;

        public StatType fromStat;

        public AlternateStatType fromAlternateStat;

        public bool toIsAlternate;

        public StatType toStat;

        public AlternateStatType toAlternateStat;

        public ScaledStatIncrease(XmlParser xml)
        {
            perAmount = xml.AtrInt("per", 1);
            gainAmount = xml.AtrInt("amount", 1);
            ParseFromStat(xml.AtrString("from", "Defense"));
            ParseToStat(xml.AtrString("to"));
        }

        private void ParseFromStat(string fromValue)
        {
            fromIsAlternate = false;
            fromStat = StatType.Defense;

            if (string.IsNullOrEmpty(fromValue))
                return;

            if (Enum.TryParse(fromValue, true, out StatType statType))
            {
                fromStat = statType;
                return;
            }

            if (Enum.TryParse(fromValue, true, out AlternateStatType alternateStatType))
            {
                fromIsAlternate = true;
                fromAlternateStat = alternateStatType;
            }
        }

        private void ParseToStat(string toValue)
        {
            toIsAlternate = false;
            toStat = StatType.Vigor;

            if (string.IsNullOrEmpty(toValue))
                return;

            if (Enum.TryParse(toValue, true, out StatType statType))
            {
                toStat = statType;
                return;
            }

            if (Enum.TryParse(toValue, true, out AlternateStatType alternateStatType))
            {
                toIsAlternate = true;
                toAlternateStat = alternateStatType;
            }
        }
    }
}
