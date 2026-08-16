using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Utils.NET.IO.Xml;
using Utils.NET.Logging;

namespace TitanCore.Data
{
    public static class DungeonSettings
    {
        private static readonly Dictionary<string, float> densities = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public static void Load(string path)
        {
            densities.Clear();
            if (!File.Exists(path))
            {
                Log.Write($"[DungeonSettings] Missing {path}, using density 1 for all dungeons", ConsoleColor.Yellow);
                return;
            }

            var root = XElement.Load(path);
            foreach (var dungeon in root.Elements("Dungeon"))
            {
                var parser = new XmlParser(dungeon);
                var name = parser.AtrString("name");
                if (string.IsNullOrEmpty(name))
                    continue;
                densities[name] = parser.Float("Density", 1f);
            }

            Log.Write($"[DungeonSettings] Loaded density for {densities.Count} dungeon(s)", ConsoleColor.Green);
        }

        public static float GetDensity(string worldName)
        {
            if (worldName != null && densities.TryGetValue(worldName, out var density))
                return Math.Max(0f, density);
            return 1f;
        }
    }
}
