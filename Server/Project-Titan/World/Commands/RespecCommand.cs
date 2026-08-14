using TitanCore.Core;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class RespecCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "respec";

        public override string Syntax => "/respec";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (player.IsAtBaseStats())
                return ChatData.Error("Your stats are already at their base values.");

            player.Respec();
            return ChatData.Info("Your stats have been reset. Essence spent on leveling was not refunded.");
        }
    }
}
