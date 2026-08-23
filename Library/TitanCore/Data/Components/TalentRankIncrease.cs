using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class TalentRankIncrease
    {
        public string node;

        public ClassType classType;

        public int amount;

        public TalentRankIncrease(XmlParser xml)
        {
            node = xml.AtrString("node", "");
            classType = xml.AtrEnum("class", (ClassType)0);
            amount = xml.AtrInt("amount", 1);
        }
    }
}
