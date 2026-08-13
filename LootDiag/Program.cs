using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Entities;
using World.Looting;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var xmlDir = Path.Combine(root, "Library", "TitanCore", "Data", "Xmls");
GameData.LoadDirectory(xmlDir, false);
DropTables.InitTables();

var enemyFile = GameDataFile.Load(Path.Combine(xmlDir, "enemies.xml"));
var demonBrute = enemyFile.infos.OfType<EnemyInfo>().First(e => e.name == "Demon Brute");
var containers = DropTables.GetLootContainers(demonBrute);

Console.WriteLine($"Demon Brute: {containers.Count} containers");
foreach (var c in containers)
    Console.WriteLine($"  {c.GetType().Name}");

// Simulate 1000 kills worth of public loot (tier 3)
var publicContainer = containers.First(c => c.GetType().Name == "PublicLoot");
var itemBags = new System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<Item>>();

int kills = 1000;
int totalItems = 0;
int weaponDrops = 0;
int armorDrops = 0;
int healDrops = 0;

var runLoot = typeof(LootContainer).GetMethod("RunLoot", BindingFlags.NonPublic | BindingFlags.Instance)!;
var variables = new PlayerLootVariables(0, 100, 1);

for (int i = 0; i < kills; i++)
{
    itemBags.Clear();
    runLoot.Invoke(publicContainer, new object[] { null!, variables, itemBags });
    if (!itemBags.TryGetValue(0, out var bag)) continue;
    totalItems += bag.Count;
    foreach (var item in bag)
    {
        var info = item.GetInfo();
        if (info.name == "Healing Spell") healDrops++;
        else if (info.slotType == SlotType.Sword || info.slotType == SlotType.Bow || info.slotType == SlotType.Claymore || info.slotType == SlotType.Spear || info.slotType == SlotType.Elixir || info.slotType == SlotType.Crossbow)
            weaponDrops++;
        else if (info.slotType == SlotType.HeavyArmor || info.slotType == SlotType.LightArmor || info.slotType == SlotType.Robe)
            armorDrops++;
    }
}

Console.WriteLine($"\nPublicLoot tier 3 simulation ({kills} kills):");
Console.WriteLine($"  Total items: {totalItems} (avg {totalItems / (float)kills:F1}/kill)");
Console.WriteLine($"  Weapons: {weaponDrops}");
Console.WriteLine($"  Armor: {armorDrops}");
Console.WriteLine($"  Healing Spell: {healDrops}");

// Print tier pool sizes for grasslands tier 3
var lootTablesField = typeof(DropTables).GetField("lootTables", BindingFlags.NonPublic | BindingFlags.Static);
var lootTables = (System.Collections.Generic.Dictionary<SoulGroup, System.Collections.Generic.List<LootContainer>>)lootTablesField!.GetValue(null)!;
var tier3 = lootTables[SoulGroup.Grasslands][3];
var lootablesField = typeof(LootContainer).GetField("lootables", BindingFlags.NonPublic | BindingFlags.Instance);
var lootables = (ILootable[])lootablesField!.GetValue(tier3)!;
var tierType = typeof(Tier);
var lootItemsField = tierType.GetField("lootItems", BindingFlags.NonPublic | BindingFlags.Instance);
var tierField = tierType.GetField("tier", BindingFlags.NonPublic | BindingFlags.Instance);
var chanceField = typeof(Loot).GetField("chance", BindingFlags.NonPublic | BindingFlags.Instance);

Console.WriteLine("\nGrasslands tier 3 loot entries:");
foreach (var loot in lootables)
{
    if (loot is Tier t)
    {
        var items = (Item[])lootItemsField!.GetValue(t)!;
        var tier = (ItemTier)tierField!.GetValue(t)!;
        var chance = (int)chanceField!.GetValue(t)!;
        Console.WriteLine($"  Tier {tier}: chance={chance}, pool={items.Length} items");
    }
    else
        Console.WriteLine($"  {loot.GetType().Name}");
}
