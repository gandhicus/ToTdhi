using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data.Components.Projectiles;
using TitanCore.Data.Items;

namespace TitanCore.Core
{
    public static class WeaponFunctions
    {
        private static Dictionary<SlotType, ushort> baseWeaponDamages = new Dictionary<SlotType, ushort>()
        {
            { SlotType.Bow, 25 },
            { SlotType.Sword, 50 },
            { SlotType.Claymore, 80 },
            { SlotType.Spear, 15 },
            { SlotType.Elixir, 25 },
            { SlotType.Crossbow, 12 },
            //{ SlotType.LancerAbility, 10 },
        };

        public static ushort GetBaseDamage(SlotType type)
        {
            if (baseWeaponDamages.TryGetValue(type, out var value))
                return value;
            return 0;
        }

        public static void GetProjectileDamage(SlotType slotType, ProjectileData data, out ushort min, out ushort max)
        {
            var baseDamage = GetBaseDamage(slotType);

            min = (ushort)(baseDamage * data.minDamageMod);
            max = (ushort)(baseDamage * data.maxDamageMod);
        }

        public static int GetVolleyShotCount(ProjectileData[] projectiles)
        {
            if (projectiles == null || projectiles.Length == 0)
                return 0;
            return Math.Max(1, projectiles[0].amount);
        }

        // Two-shot swords store both slashes in the volley; summing them so ability
        // scaling is not stuck on the first projectile alone.
        public static void GetVolleyDamage(SlotType slotType, ProjectileData[] projectiles, out int min, out int max)
        {
            min = 0;
            max = 0;
            int count = GetVolleyShotCount(projectiles);
            for (int i = 0; i < count; i++)
            {
                var shot = projectiles[i % projectiles.Length];
                GetProjectileDamage(slotType, shot, out var shotMin, out var shotMax);
                min += shotMin;
                max += shotMax;
            }
        }
    }
}
