using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Items;
using Utils.NET.Utils;

namespace World.Looting
{
    public class GateKeys : Loot, ILootable
    {
        private Item[] lootItems;

        public GateKeys(int chance) : base(chance)
        {
            lootItems = GameData.objects.Values
                .Where(_ => _ is GateKeyInfo key && key.droppable)
                .Select(_ => new Item(_.id, false, 1))
                .ToArray();
        }

        public void AddItems(List<Item> items, PlayerLootVariables variables)
        {
            if (lootItems.Length == 0) return;
            if (DoChance(variables.damagePercent))
                items.Add(lootItems.Random());
        }
    }
}
