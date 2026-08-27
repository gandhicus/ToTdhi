using System.Collections.Generic;
using TitanCore.Data;
using Utils.NET.Logging;
using World.Map.Spawning;

namespace World.Worlds.Gates
{
    /// <summary>
    /// A dungeon built entirely from a dungeons.xml entry, with no dedicated C# class.
    ///
    /// Most of the existing dungeons are, structurally, just "load this map file, play
    /// this music, put this boss on that marker" - Mannah's Fortress is barely more than
    /// that. Every one of those needed its own class purely to hold a handful of
    /// constants, which meant adding a dungeon always meant writing and compiling C#.
    /// This class supplies those constants from data instead.
    ///
    /// What this deliberately does NOT do is replace the procedural dungeons. Dumir,
    /// Valdok's Forge and Bhognin's Gate generate their maps in code, with landmasses
    /// and distance fields, and there is no sensible way to express that in XML. They
    /// keep their own classes and are unaffected by any of this.
    /// </summary>
    public class DataDrivenGate : Gate
    {
        /// <summary>
        /// The definition is supplied at construction and never changes. It is read-only
        /// because the same definition object is shared by every instance of this dungeon
        /// that is ever opened - mutating it would leak state between runs.
        /// </summary>
        private readonly DungeonDefinition definition;

        public DataDrivenGate(DungeonDefinition definition)
        {
            this.definition = definition;
        }

        // These properties are read by the base World and Gate classes during
        // InitWorld, which always runs after construction, so definition is set by then.
        public override string WorldName => definition.name;

        public override ushort PreferredPortal => definition.portal;

        protected override string MapFile => definition.mapFile;

        protected override string DefaultMusic => definition.music;

        public override int MaxPlayerCount => definition.maxPlayers;

        protected override int PortalTime => definition.portalTime;

        protected override int TargetPlayers => definition.targetPlayers;

        protected override float ScalePerPlayer => definition.scalePerPlayer;

        public override bool LimitSight => definition.limitSight;

        public override bool AllowPlayerTeleport => definition.allowPlayerTeleport;

        /// <summary>
        /// Turns the definition's boss list into the dungeon's objectives, in order.
        ///
        /// This runs after the map has loaded, so region markers are available. Every
        /// lookup is checked because the map and the XML are edited independently: the
        /// XML can easily name a marker the map author never painted, and
        /// World.GetRegions returns null in that case rather than an empty list.
        /// A boss we cannot place is skipped with an explanation instead of crashing
        /// the dungeon on entry.
        /// </summary>
        protected override QuestTaskSystem CreateTasks()
        {
            if (definition.bosses.Count == 0)
                return null; // no objectives; the dungeon just stays open until its portal expires

            var tasks = new List<QuestTask>();

            foreach (var boss in definition.bosses)
            {
                var markers = GetRegions(boss.region);
                if (markers == null || markers.Count == 0)
                {
                    Log.Error($"[DataDrivenGate] '{definition.name}' wants a boss at region '{boss.region}' but the map '{definition.mapFile}' has no such marker - skipping that objective.");
                    continue;
                }

                if (boss.regionIndex < 0 || boss.regionIndex >= markers.Count)
                {
                    Log.Error($"[DataDrivenGate] '{definition.name}' asks for '{boss.region}' marker number {boss.regionIndex}, but the map only has {markers.Count} - using the first one.");
                    tasks.Add(new BossTask(boss.type, markers[0].ToVec2() + 0.5f));
                    continue;
                }

                // The 0.5f centres the boss on the tile rather than its corner, matching
                // what the hand-written dungeons do.
                tasks.Add(new BossTask(boss.type, markers[boss.regionIndex].ToVec2() + 0.5f));
            }

            if (tasks.Count == 0)
            {
                Log.Error($"[DataDrivenGate] '{definition.name}' ended up with no usable objectives, so it cannot be completed. Check the region markers on its map.");
                return null;
            }

            return new QuestTaskSystem(this, tasks.ToArray());
        }
    }
}
