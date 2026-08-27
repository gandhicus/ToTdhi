using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using TitanCore.Core;
using Utils.NET.IO.Xml;
using Utils.NET.Logging;

namespace TitanCore.Data
{
    /// <summary>
    /// Reads dungeons.xml.
    ///
    /// The file serves two purposes, described in full on DungeonDefinition: it tunes
    /// enemy density for dungeons that already exist, and it can define entire dungeons
    /// that need no C# class.
    ///
    /// Failure policy here is deliberately forgiving. A broken dungeon entry costs you
    /// that one dungeon, not the server: a missing file leaves every density at 1, and a
    /// malformed entry is skipped with an error naming it. That is the opposite of
    /// GameData's all-or-nothing rule, and the reason is that item and enemy definitions
    /// are a shared contract with the client, whereas dungeon settings are server-only
    /// and cannot desync anything.
    /// </summary>
    public static class DungeonSettings
    {
        private static readonly Dictionary<string, float> densities = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<DungeonDefinition> definitions = new List<DungeonDefinition>();

        /// <summary>
        /// Every dungeon entry that fully defines a dungeon. The server walks this at
        /// startup and registers each one.
        /// </summary>
        public static IReadOnlyList<DungeonDefinition> Definitions => definitions;

        public static void Load(string path)
        {
            densities.Clear();
            definitions.Clear();

            if (!File.Exists(path))
            {
                Log.Write($"[DungeonSettings] Missing {path}, using density 1 for all dungeons", ConsoleColor.Yellow);
                return;
            }

            XElement root;
            try
            {
                root = XElement.Load(path);
            }
            catch (Exception e)
            {
                // Malformed XML at the file level. Consequence: all dungeons run at
                // density 1 and no data-driven dungeons exist. The server still starts.
                Log.Error($"[DungeonSettings] Could not read {path}: {e.Message}. Using density 1 for all dungeons and loading no data-driven dungeons.");
                return;
            }

            int dataDriven = 0;

            foreach (var dungeon in root.Elements("Dungeon"))
            {
                var parser = new XmlParser(dungeon);
                var name = parser.AtrString("name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    Log.Error("[DungeonSettings] A <Dungeon> entry has no 'name' attribute - skipping it.");
                    continue;
                }

                try
                {
                    var definition = ParseDungeon(parser, name);

                    // Density is recorded for every entry, data-driven or not, because
                    // even a hand-written C# dungeon reads its density from here.
                    densities[name] = definition.density;

                    if (definition.IsDataDriven)
                    {
                        definitions.Add(definition);
                        dataDriven++;
                    }
                    else if (!string.IsNullOrWhiteSpace(definition.key) || !string.IsNullOrWhiteSpace(definition.mapFile))
                    {
                        // Half-configured: the author clearly intended a full dungeon but
                        // left out one of the two required pieces. Worth saying loudly,
                        // because otherwise it silently degrades into a density-only
                        // entry and the dungeon simply never appears.
                        Log.Error($"[DungeonSettings] '{name}' has only one of <Key> and <MapFile>. Both are required to define a dungeon, so this entry only sets density.");
                    }
                }
                catch (Exception e)
                {
                    // One bad entry should not cost the others.
                    Log.Error($"[DungeonSettings] Failed to read dungeon '{name}': {e.Message} - skipping it.");
                }
            }

            Log.Write($"[DungeonSettings] Loaded {densities.Count} dungeon setting(s), {dataDriven} of them fully data-driven", ConsoleColor.Green);
        }

        private static DungeonDefinition ParseDungeon(XmlParser parser, string name)
        {
            var definition = new DungeonDefinition
            {
                name = name,
                density = parser.Float("Density", 1f),
                key = parser.String("Key", null),
                mapFile = parser.String("MapFile", null),
                portal = (ushort)parser.Hex("Portal", 0),
                music = parser.String("Music", ""),
                maxPlayers = parser.Int("MaxPlayers", 20),
                portalTime = parser.Int("PortalTime", -1),
                targetPlayers = parser.Int("TargetPlayers", 2),
                scalePerPlayer = parser.Float("ScalePerPlayer", 0.2f),
                limitSight = parser.Exists("LimitSight"),
                allowPlayerTeleport = parser.Exists("AllowPlayerTeleport")
            };

            foreach (var boss in parser.Elements("Boss"))
            {
                var type = (ushort)boss.AtrHex("id", 0);
                if (type == 0)
                {
                    Log.Error($"[DungeonSettings] '{name}' has a <Boss> with no valid 'id' - skipping that objective.");
                    continue;
                }

                definition.bosses.Add(new DungeonBossDefinition
                {
                    type = type,
                    region = boss.AtrEnum("region", Region.Tag1),
                    regionIndex = boss.AtrInt("index", 0)
                });
            }

            return definition;
        }

        public static float GetDensity(string worldName)
        {
            if (worldName != null && densities.TryGetValue(worldName, out var density))
                return Math.Max(0f, density);
            return 1f;
        }
    }
}
