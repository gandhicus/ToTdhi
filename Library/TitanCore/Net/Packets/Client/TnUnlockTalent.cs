using Utils.NET.IO;

namespace TitanCore.Net.Packets.Client
{
    public class TnUnlockTalent : TnPacket
    {
        public override TnPacketType Type => TnPacketType.UnlockTalent;

        public byte nodeIndex;

        public TnUnlockTalent()
        {
        }

        public TnUnlockTalent(byte nodeIndex)
        {
            this.nodeIndex = nodeIndex;
        }

        protected override void Read(BitReader r)
        {
            nodeIndex = r.ReadUInt8();
        }

        protected override void Write(BitWriter w)
        {
            w.Write(nodeIndex);
        }
    }
}
