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
            if (player.IsAtBaseStats() && player.GetLevel() == player.GetStatLevel())
                return ChatData.Error("Your stats and skills are already at their base values.");

            if (!player.Respec())
                return ChatData.Error("Unable to unequip your talisman. Your stats and skills were not reset.");
            return ChatData.Info("Your stats and skills have been reset. Essence spent on leveling and the skill tree was not refunded.");
        }
    }
}
