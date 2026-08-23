using TitanCore.Core;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class RespecSkillsCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "respecskills";

        public override string Syntax => "/respecskills";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            return player.RespecSkills();
        }
    }
}
