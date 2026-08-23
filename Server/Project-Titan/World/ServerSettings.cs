using System;
using System.Collections.Generic;
using System.IO;
using Utils.NET.Logging;

namespace World
{
    public static class ServerSettings
    {
        private const string Settings_File = "ServerSettings.txt";

        private const string Admin_Env = "TRIALS_SERVER_ADMIN";

        private const string Anticheat_Env = "TRIALS_ANTICHEAT";

        private const string Loot_Boost_Env = "TRIALS_LOOT_BOOST";

        public static readonly bool AdminCommands = GetBool("admin", Admin_Env, false);

        public static readonly bool AntiCheat = GetBool("anticheat", Anticheat_Env, false);

        public static readonly int LootBoost = Clamp(GetInt("lootBoost", Loot_Boost_Env, 1), 1, 100);

        static ServerSettings()
        {
            Log.Write($"Server settings: admin={AdminCommands}, anticheat={AntiCheat}, lootBoost={LootBoost}");
        }

        private static bool GetBool(string key, string envName, bool defaultValue)
        {
            var value = GetValue(key, envName);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim();
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                value == "1")
                return true;

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("off", StringComparison.OrdinalIgnoreCase) ||
                value == "0")
                return false;

            return defaultValue;
        }

        private static int GetInt(string key, string envName, int defaultValue)
        {
            var value = GetValue(key, envName);
            if (int.TryParse(value, out var parsed))
                return parsed;

            return defaultValue;
        }

        private static string GetValue(string key, string envName)
        {
            var envValue = Environment.GetEnvironmentVariable(envName);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue;

            foreach (var path in GetSettingsPaths())
            {
                var value = ReadValue(path, key);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static IEnumerable<string> GetSettingsPaths()
        {
            var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in new string[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;

                foreach (var path in GetSettingsPaths(root))
                {
                    if (yielded.Add(path))
                        yield return path;
                }
            }
        }

        private static IEnumerable<string> GetSettingsPaths(string root)
        {
            yield return Path.Combine(root, Settings_File);

            var parent = Directory.GetParent(root);
            if (parent != null)
                yield return Path.Combine(parent.FullName, Settings_File);
        }

        private static string ReadValue(string path, string key)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var separatorIndex = GetSeparatorIndex(line);
                    if (separatorIndex <= 0)
                        continue;

                    var lineKey = line.Substring(0, separatorIndex).Trim();
                    if (!lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return line.Substring(separatorIndex + 1).Trim();
                }
            }
            catch (Exception e)
            {
                Log.Write($"Failed to read {path}: {e.Message}");
            }

            return null;
        }

        private static int GetSeparatorIndex(string line)
        {
            var colonIndex = line.IndexOf(':');
            var equalsIndex = line.IndexOf('=');

            if (colonIndex < 0)
                return equalsIndex;
            if (equalsIndex < 0)
                return colonIndex;
            return Math.Min(colonIndex, equalsIndex);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
