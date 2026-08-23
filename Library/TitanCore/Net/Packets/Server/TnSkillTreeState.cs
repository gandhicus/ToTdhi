using TitanCore.Core;
using Utils.NET.IO;

namespace TitanCore.Net.Packets.Server
{
    public class TnSkillTreeState : TnPacket
    {
        public override TnPacketType Type => TnPacketType.SkillTreeState;

        public uint packedRanks;

        public Item talisman;

        public TnSkillTreeState()
        {
            talisman = Item.Blank;
        }

        public TnSkillTreeState(uint packedRanks, Item talisman)
        {
            this.packedRanks = packedRanks;
            this.talisman = talisman;
        }

        protected override void Read(BitReader r)
        {
            packedRanks = r.ReadUInt32();
            talisman = Item.ReadItem(r);
        }

        protected override void Write(BitWriter w)
        {
            w.Write(packedRanks);
            talisman.Write(w);
        }
    }
}
