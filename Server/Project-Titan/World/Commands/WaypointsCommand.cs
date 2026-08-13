using System;
using System.Collections.Generic;
using TitanCore.Core;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class WaypointsCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "waypoints";

        public override string Syntax => "/waypoints";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (!(player.world is Worlds.Overworld overworld) || overworld.waypointSystem == null)
                return ChatData.Error("Waypoints are only available in the overworld.");

            if (overworld.waypointSystem.waypoints.Count == 0)
                return ChatData.Info("No waypoints are currently spawned.");

            foreach (var waypoint in overworld.waypointSystem.waypoints)
            {
                var pos = waypoint.position.Value;
                player.AddChat(ChatData.Info($"{waypoint.waypointName.Value} ({(int)pos.x},{(int)pos.y}) id {waypoint.gameId}"));
            }

            return null;
        }
    }
}
