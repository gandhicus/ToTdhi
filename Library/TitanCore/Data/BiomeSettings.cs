using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Utils.NET.Logging;

namespace TitanCore.Data
{
    /// <summary>
    /// Reads overworld enemy multipliers from biomes.xml. Missing or invalid entries
    /// retain the original spawn behavior so a settings error does not stop the server.
    /// </summary>
    public static class BiomeSettings
    {
        private static readonly Dictionary<string, (float density, float respawnRate)> settings =
            new Dictionary<string, (float, float)>(StringComparer.OrdinalIgnoreCase);

        public static void Load(string path)
        {
            settings.Clear();
            if (!File.Exists(path))
            {
                Log.Write($"[BiomeSettings] Missing {path}, using multiplier 1 for all biomes", ConsoleColor.Yellow);
                return;
            }

            try
            {
                var root = XElement.Load(path);
                if (root.Name != "Biomes")
                    throw new InvalidDataException("Expected a <Biomes> root element");

                foreach (var biome in root.Elements("Biome"))
                {
                    var name = (string)biome.Attribute("name");
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Log.Error("[BiomeSettings] A <Biome> entry has no name; skipping it.");
                        continue;
                    }

                    if (!TryMultiplier(biome.Element("Density"), out var density) ||
                        !TryMultiplier(biome.Element("RespawnRate"), out var respawnRate))
                    {
                        Log.Error($"[BiomeSettings] '{name}' has an invalid multiplier; using defaults for that biome.");
                        continue;
                    }

                    settings[name] = (density, respawnRate);
                }
            }
            catch (Exception e)
            {
                // A damaged file must not leave a partly loaded set of multipliers.
                settings.Clear();
                Log.Error($"[BiomeSettings] Could not read {path}: {e.Message}. Using defaults for all biomes.");
            }
        }

        private static bool TryMultiplier(XElement element, out float value)
        {
            value = 1f;
            if (element == null) return true;
            return float.TryParse(element.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value) &&
                !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }

        public static float GetDensity(string biomeName)
        {
            return biomeName != null && settings.TryGetValue(biomeName, out var value) ? value.density : 1f;
        }

        public static float GetRespawnRate(string biomeName)
        {
            return biomeName != null && settings.TryGetValue(biomeName, out var value) ? value.respawnRate : 1f;
        }
    }
}
