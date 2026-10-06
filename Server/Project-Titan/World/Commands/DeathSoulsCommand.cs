using System.Linq;
using TitanCore.Core;
using TitanCore.Data.Entities;
using TitanCore.Net;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class DeathSoulsCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "deathsouls";

        public override string Syntax => "/deathsouls";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (args.args.Length != 0)
                return SyntaxError;

            // A live character is needed to read the lifetime total used by the death payout.
            if (player.character == null || !(player.info is CharacterInfo charInfo))
                return ChatData.Error("Unable to calculate death souls for this character right now.");

            var soulsEarned = player.GetStatisticValue(CharacterStatisticType.SoulsEarned);
            var baseReward = NetConstants.GetBaseDeathReward(charInfo, soulsEarned);
            var reward = new DeathReward(baseReward, player.character.statistics.Values.ToArray());

            // The statistic uses thousandths of the essence shown to players.
            return ChatData.Info($"Death souls if you died now: {reward.totalReward:N0}. Lifetime essence earned: {soulsEarned / 1000:N0}.");
        }
    }
}
