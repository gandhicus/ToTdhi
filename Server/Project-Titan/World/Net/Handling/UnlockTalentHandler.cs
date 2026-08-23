using TitanCore.Core;
using TitanCore.Net;
using TitanCore.Net.Packets.Client;
using TitanCore.Net.Packets.Models;

namespace World.Net.Handling
{
    public class UnlockTalentHandler : ClientPacketHandler<TnUnlockTalent>
    {
        public override void Handle(TnUnlockTalent packet, Client connection)
        {
            if (!SkillTreeFunctions.IsEnabled)
                return;
            if (packet.nodeIndex >= SkillTreeFunctions.Node_Count)
                return;
            if (!connection.player.TryUnlockTalent((SkillTreeNode)packet.nodeIndex, out var error) && error != null)
            {
                connection.player.AddChat(ChatData.Error(error));
                connection.player.SendSkillTreeState();
            }
            else
                connection.player.SendSkillTreeState();
        }
    }
}
