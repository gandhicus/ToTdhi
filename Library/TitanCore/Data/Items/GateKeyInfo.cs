using Utils.NET.IO.Xml;

namespace TitanCore.Data.Items
{
    public class GateKeyInfo : ItemInfo
    {
        public override GameObjectType Type => GameObjectType.GateKey;

        public string gateType;

        public bool droppable;

        public override void Parse(XmlParser xml)
        {
            base.Parse(xml);

            gateType = xml.String("Gate");
            droppable = xml.Exists("Droppable");
        }
    }
}
