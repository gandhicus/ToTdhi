using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Entities;
using TitanCore.Data.Items;
using World.Looting;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var xmlDir = Path.Combine(root, "Library", "TitanCore", "Data", "Xmls");
GameData.LoadDirectory(xmlDir, false);
DropTables.InitTables();

var runLoot = typeof(LootContainer).GetMethod("RunLoot", BindingFlags.NonPublic | BindingFlags.Instance)!;
var lootablesField = typeof(LootContainer).GetField("lootables", BindingFlags.NonPublic | BindingFlags.Instance)!;
var lootItemsField = typeof(Tier).GetField("lootItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
var singleItemsField = typeof(World.Looting.Single).GetField("lootItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
var chanceField = typeof(Loot).GetField("chance", BindingFlags.NonPublic | BindingFlags.Instance)!;
var maxPercentField = typeof(SoulboundLoot).GetField("maxPercent");

var dumirEnemies = GameData.objects.Values
    .OfType<EnemyInfo>()
    .Where(e => e.soulGroup == SoulGroup.Dumir)
    .OrderBy(e => e.name)
    .ToList();

Console.WriteLine("=== Dumir enemies and loot tiers ===");
foreach (var enemy in dumirEnemies)
{
    var tiers = enemy.lootTiers.Length == 0 ? "(none)" : string.Join(", ", enemy.lootTiers);
    Console.WriteLine($"  {enemy.name}: LootTiers [{tiers}]  titan={enemy.titan}");
}

Console.WriteLine("\n=== Dumir drop table slots ===");
var lootTablesField = typeof(DropTables).GetField("lootTables", BindingFlags.NonPublic | BindingFlags.Static)!;
var lootTables = (Dictionary<SoulGroup, List<LootContainer>>)lootTablesField.GetValue(null)!;
var dumirTable = lootTables[SoulGroup.Dumir];
for (int i = 0; i < dumirTable.Count; i++)
{
    var container = dumirTable[i];
    var kind = container.GetType().Name;
    var extra = "";
    if (container is SoulboundLoot sb)
        extra = $" maxPercent={sb.maxPercent}";
    Console.WriteLine($"  [{i}] {kind}{extra}");
    foreach (var lootable in (ILootable[])lootablesField.GetValue(container)!)
    {
        if (lootable is Tier t)
        {
            var items = (Item[])lootItemsField.GetValue(t)!;
            var chance = (int)chanceField.GetValue(t)!;
            var broken = CountBroken(items);
            Console.WriteLine($"      Tier pool chance={chance / 10000.0:0.##}%  items={items.Length}  broken={broken}");
            if (items.Length == 0)
                Console.WriteLine("        EMPTY POOL (this roll can never drop)");
        }
        else if (lootable is World.Looting.Single s)
        {
            var items = (Item[])singleItemsField.GetValue(s)!;
            var chance = (int)chanceField.GetValue(s)!;
            var names = string.Join(", ", items.Select(DescribeItem));
            Console.WriteLine($"      Single chance={chance / 10000.0:0.##}%  [{names}]");
        }
        else
        {
            Console.WriteLine($"      {lootable.GetType().Name}");
        }
    }
}

var dungeonBosses = new[] { "Oda", "Beorn", "Raeg", "Yolma", "Balun" }
    .Select(name => dumirEnemies.First(e => e.name == name))
    .ToArray();

const int runs = 50;
var perRunDrops = new List<List<(string source, Item item, string status)>>();
var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
int keyDrops = 0;
int brokenDrops = 0;
int blankDrops = 0;
var brokenDetails = new List<string>();

Console.WriteLine($"\n=== Simulating {runs} Dumir dungeon completions ===");
Console.WriteLine("(Oda, Beorn, Raeg, Yolma, Balun — solo 100% damage, lootBoost=1)\n");

for (int run = 1; run <= runs; run++)
{
    var runDrops = new List<(string source, Item item, string status)>();
    foreach (var boss in dungeonBosses)
    {
        var bags = new Dictionary<ulong, List<Item>>();
        foreach (var container in DropTables.GetLootContainers(boss))
        {
            ulong ownerId = container is PublicLoot ? 0UL : 1UL;
            runLoot.Invoke(container, new object[] { null!, new PlayerLootVariables(ownerId, 100, 1f), bags });
        }

        foreach (var bag in bags.Values)
        {
            foreach (var item in bag)
            {
                var status = Classify(item, out var displayName);
                runDrops.Add((boss.name, item, status));
                totals[displayName] = totals.GetValueOrDefault(displayName) + 1;
                if (displayName.Contains("Key", StringComparison.OrdinalIgnoreCase))
                    keyDrops++;
                if (status != "ok")
                {
                    brokenDrops++;
                    brokenDetails.Add($"Run {run} {boss.name}: {status} id=0x{item.id:x} name={displayName}");
                }
                if (item.IsBlank)
                    blankDrops++;
            }
        }
    }
    perRunDrops.Add(runDrops);
}

for (int i = 0; i < perRunDrops.Count; i++)
{
    var drops = perRunDrops[i];
    if (drops.Count == 0)
    {
        Console.WriteLine($"Run {i + 1,2}: (no drops)");
        continue;
    }
    var grouped = drops.GroupBy(d => $"{d.source}:{DescribeItem(d.item)}")
        .Select(g => g.Count() == 1 ? g.Key : $"{g.Key} x{g.Count()}");
    Console.WriteLine($"Run {i + 1,2}: {string.Join(" | ", grouped)}");
}

Console.WriteLine("\n=== Totals across 50 Dumirs ===");
foreach (var kv in totals.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
    Console.WriteLine($"  {kv.Value,4}  {kv.Key}");

Console.WriteLine($"\nTotal items: {totals.Values.Sum()}");
Console.WriteLine($"Key drops: {keyDrops}");
Console.WriteLine($"Blank (id=0) drops: {blankDrops}");
Console.WriteLine($"Broken drops: {brokenDrops}");
if (brokenDetails.Count == 0)
    Console.WriteLine("No broken item drops detected (all ids resolved to ItemInfo, none blank, none TEST).");
else
{
    Console.WriteLine("Broken drop details:");
    foreach (var line in brokenDetails)
        Console.WriteLine("  " + line);
}

Console.WriteLine("\n=== Key drop math (Balun slot 14) ===");
Console.WriteLine("Key to Dumir: SoulboundLoot maxPercent=0.1, Chance(20) => 20% at 100% damage");
Console.WriteLine($"Expected in 50 Balun kills: ~{50 * 0.20:0.0}");
Console.WriteLine("Key to Valdok's Forge is Dumir table slot 15; Aldrite's Gamble is slot 16.");
Console.WriteLine("Neither is on Oda/Beorn/Raeg/Yolma/Balun lootTiers, so they cannot drop in this dungeon.");

static string DescribeItem(Item item)
{
    try
    {
        if (item.IsBlank) return "<BLANK id=0>";
        if (!GameData.objects.TryGetValue(item.id, out var obj))
            return $"<MISSING id=0x{item.id:x}>";
        if (obj is not ItemInfo info)
            return $"<NOT ITEM {obj.GetType().Name} '{obj.name}' id=0x{item.id:x}>";
        return info.name;
    }
    catch (Exception ex)
    {
        return $"<THROW {ex.GetType().Name}: {ex.Message}>";
    }
}

static string Classify(Item item, out string displayName)
{
    displayName = DescribeItem(item);
    if (item.IsBlank) return "blank";
    if (displayName.StartsWith("<")) return "broken-lookup";
    if (displayName.StartsWith("TEST", StringComparison.OrdinalIgnoreCase)) return "test-item";
    try
    {
        var info = item.GetInfo();
        if (info == null) return "null-info";
    }
    catch (Exception ex)
    {
        displayName = $"<GetInfo {ex.GetType().Name}>";
        return "getinfo-throw";
    }
    return "ok";
}

static int CountBroken(Item[] items)
{
    int n = 0;
    foreach (var item in items)
    {
        if (item.IsBlank || !GameData.objects.TryGetValue(item.id, out var obj) || obj is not ItemInfo)
            n++;
    }
    return n;
}
