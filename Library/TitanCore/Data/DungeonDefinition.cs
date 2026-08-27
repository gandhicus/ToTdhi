using System.Collections.Generic;
using TitanCore.Core;

namespace TitanCore.Data
{
    /// <summary>
    /// One boss objective in a data-driven dungeon.
    ///
    /// The boss's position is given as a map region marker rather than raw coordinates,
    /// so the map author decides where the boss stands by painting a marker rather than
    /// by anyone editing numbers in a file.
    /// </summary>
    public class DungeonBossDefinition
    {
        /// <summary>
        /// Hex id of the enemy to spawn, from enemies.xml.
        /// </summary>
        public ushort type;

        /// <summary>
        /// Which region marker on the map the boss spawns at.
        /// </summary>
        public Region region = Region.Tag1;

        /// <summary>
        /// Which marker to use when the map has several of the same region type.
        /// Defaults to the first.
        /// </summary>
        public int regionIndex = 0;
    }

    /// <summary>
    /// A dungeon as described by dungeons.xml.
    ///
    /// This type covers two different jobs, which is worth understanding:
    ///
    /// 1. Every entry can carry a Density value, which tunes enemy counts for a dungeon
    ///    that already exists. This is the original purpose of dungeons.xml and applies
    ///    to the hand-written C# dungeons too.
    ///
    /// 2. An entry that also supplies a Key and a MapFile *defines a whole dungeon*,
    ///    with no C# class needed. Those are the data-driven dungeons.
    ///
    /// Keeping both in one file means a designer has a single place to look, and old
    /// density-only entries keep working untouched.
    /// </summary>
    public class DungeonDefinition
    {
        /// <summary>
        /// Display name, shown on the portal. For a density-only entry this must match
        /// the existing dungeon's WorldName, which is how the two are linked.
        /// </summary>
        public string name;

        /// <summary>
        /// The short key used by create_gate actions and gate key items. Required for a
        /// data-driven dungeon; absent on density-only entries.
        /// </summary>
        public string key;

        /// <summary>
        /// Enemy spawn multiplier. 1 = as designed, 0.5 = half, 0 = none.
        /// </summary>
        public float density = 1f;

        /// <summary>
        /// Map file under Map\Files\. Required for a data-driven dungeon.
        /// </summary>
        public string mapFile;

        /// <summary>
        /// Hex id of the portal object players click to enter.
        /// </summary>
        public ushort portal;

        /// <summary>
        /// Music track name.
        /// </summary>
        public string music = "";

        public int maxPlayers = 20;

        /// <summary>
        /// How many seconds the entry portal stays open. -1 keeps it open until the
        /// dungeon is finished.
        /// </summary>
        public int portalTime = -1;

        /// <summary>
        /// The player count the dungeon is balanced around. Enemy health scales up or
        /// down from here by scalePerPlayer.
        /// </summary>
        public int targetPlayers = 2;

        public float scalePerPlayer = 0.2f;

        public bool limitSight = false;

        public bool allowPlayerTeleport = false;

        /// <summary>
        /// Boss objectives, completed in the order listed.
        /// </summary>
        public List<DungeonBossDefinition> bosses = new List<DungeonBossDefinition>();

        /// <summary>
        /// True when this entry describes a complete dungeon rather than just tuning an
        /// existing one. Both a key and a map file are required: a key with no map would
        /// produce a dungeon with nothing to load, and a map with no key would produce a
        /// dungeon nothing can open.
        /// </summary>
        public bool IsDataDriven =>
            !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(mapFile);
    }
}
