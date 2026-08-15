using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data.Items;

namespace TitanCore.Core
{
    public static class ItemFunctions
    {
        public static int GetEquippedAlternateStat(Item[] equips, AlternateStatType type)
        {
            if (equips == null) return 0;

            int total = 0;
            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].IsBlank) continue;
                if (equips[i].GetInfo() is EquipmentInfo equip)
                {
                    var alternateStats = EquipmentStatFunctions.GetAlternateStatIncreases(equips[i], equip);
                    if (alternateStats.TryGetValue(type, out var amount))
                        total += amount;
                }
            }
            return total;
        }
        /*
        public static int RateOfFireMs(SlotType type)
        {
            switch (type)
            {
                case SlotType.Sword:
                    return 500;
                case SlotType.Claymore:
                    return 700;
                case SlotType.Bow:
                    return 350;
                case SlotType.Spear:
                    return 400;
                default:
                    return 0;
            }
        }
        */
    }
}
