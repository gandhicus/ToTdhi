using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data;
using TitanCore.Net.Packets.Models;
using World.GameState;

namespace World.Map.Objects.Map
{
    public class Waypoint : GameObject
    {
        public override GameObjectType Type => GameObjectType.Waypoint;

        public override bool Ticks => false;

        public override bool Global => true;

        public override bool Teleportable => true;

        public ObjectStat<string> waypointName = new ObjectStat<string>(ObjectStatType.Name, ObjectStatScope.Public, "", "");

        protected override void GetStats(List<ObjectStat> list)
        {
            base.GetStats(list);

            list.Add(waypointName);
        }
    }
}
