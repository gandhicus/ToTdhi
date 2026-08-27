using System;
using System.Collections.Generic;
using System.Linq;
using Utils.NET.Logging;

namespace World.Worlds.Gates
{
    /// <summary>
    /// The single place that maps a short dungeon key (the string designers write in
    /// scripts and item definitions) to the code that builds that dungeon.
    ///
    /// Why this exists: this mapping used to be duplicated as two hand-written switch
    /// statements, one in the CreateGate death action and one in GateSpawner. Adding a
    /// dungeon to only one of them produced a dungeon that could be opened by a key but
    /// not by killing a boss, or the reverse - a bug with no error message at all.
    /// There is now exactly one list.
    ///
    /// Entries are factories rather than Types because data-driven dungeons (defined in
    /// dungeons.xml rather than in C#) all share a single class and need their specific
    /// definition handed to them at construction time. A plain Type plus
    /// Activator.CreateInstance cannot express that.
    /// </summary>
    public static class GateRegistry
    {
        /// <summary>
        /// Key -> factory. Case-insensitive so "Forge" and "forge" behave the same, which
        /// matters because these keys are typed by hand into XML and .ls scripts.
        /// </summary>
        private static readonly Dictionary<string, Func<Gate>> factories =
            new Dictionary<string, Func<Gate>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The dungeons implemented as C# classes. Data-driven dungeons are added later,
        /// during server startup, once dungeons.xml has been read.
        /// </summary>
        static GateRegistry()
        {
            factories["forge"] = () => new ValdoksForge();
            factories["dumir"] = () => new Dumir();
            factories["bubra"] = () => new BhogninsGate();
            factories["woods"] = () => new RictornsGate();
            factories["fortress"] = () => new MannahsFortress();
        }

        /// <summary>
        /// Adds a dungeon to the registry. Used by the data-driven dungeon loader.
        ///
        /// A key that is already taken is refused rather than overwritten: the built-in
        /// C# dungeons should always win over an XML entry, otherwise a typo in
        /// dungeons.xml could silently replace a real dungeon with a broken one.
        /// </summary>
        public static bool Register(string key, Func<Gate> factory)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Log.Error("[GateRegistry] Refusing to register a dungeon with no key.");
                return false;
            }

            if (factory == null)
            {
                Log.Error($"[GateRegistry] Refusing to register dungeon '{key}' with no factory.");
                return false;
            }

            if (factories.ContainsKey(key))
            {
                Log.Error($"[GateRegistry] Dungeon key '{key}' is already registered - keeping the existing one.");
                return false;
            }

            factories[key] = factory;
            return true;
        }

        /// <summary>
        /// Returns true if a dungeon is registered under this key. Used to validate
        /// content at startup instead of waiting for a player to find the problem.
        /// </summary>
        public static bool IsRegistered(string key)
        {
            return key != null && factories.ContainsKey(key);
        }

        /// <summary>
        /// Builds a new instance of the dungeon registered under the given key, or null
        /// if the key is unknown.
        ///
        /// Callers must handle null. An unknown key means content referenced a dungeon
        /// that does not exist - a gate key item with a bad Gate value, or a
        /// create_gate action with a bad type. The caller decides how to report it,
        /// because the useful message differs (a player-facing chat error versus a log
        /// line), but in no case should it crash the world.
        /// </summary>
        public static Gate Create(string key)
        {
            if (key == null || !factories.TryGetValue(key, out var factory))
                return null;

            try
            {
                return factory.Invoke();
            }
            catch (Exception e)
            {
                // A data-driven dungeon whose definition is malformed can throw here.
                // Consequence: the portal simply never opens, and the world the player
                // is standing in carries on unaffected.
                Log.Error($"[GateRegistry] Failed to construct dungeon '{key}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Every registered key, sorted. Used for startup logging so the console shows
        /// what content actually loaded.
        /// </summary>
        public static IEnumerable<string> Keys => factories.Keys.OrderBy(_ => _);
    }
}
