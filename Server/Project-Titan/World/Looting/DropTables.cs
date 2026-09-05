using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Core;
using TitanCore.Data.Entities;

namespace World.Looting
{
    public static class DropTables
    {
        private static Dictionary<SoulGroup, List<LootContainer>> lootTables;

        public static void InitTables()
        {
            lootTables = new Dictionary<SoulGroup, List<LootContainer>>()
            {
                { SoulGroup.OceanBeach, new List<LootContainer>
                {
                    new PublicLoot(
                        new Single(Loot.Chance(8), new Item("Healing Spell"))
                        ),
                    new PublicLoot(
                        Tier.Weapon(Loot.Chance(30), ItemTier.Tier1),
                        Tier.Armor(Loot.Chance(15), ItemTier.Tier1),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                } },

                { SoulGroup.Grasslands, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 1
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier2),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier2),
                        Tier.Accessory(Loot.Chance(3), ItemTier.Tier1),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 2
                        Tier.Weapon(Loot.Chance(35), ItemTier.Tier2),
                        Tier.Armor(Loot.Chance(20), ItemTier.Tier2),
                        Tier.Accessory(Loot.Chance(3), ItemTier.Tier1),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier2),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier2),
                        Tier.Weapon(Loot.Chance(50), ItemTier.Tier3),
                        Tier.Armor(Loot.Chance(35), ItemTier.Tier3),
                        Tier.Accessory(Loot.Chance(15), ItemTier.Tier1),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new SoulboundLoot(1, // 4
                        new Single(Loot.Chance(8), new Item("Tear of Life"))
                        ),
                } },

                { SoulGroup.DarkForest, new List<LootContainer>
                {
                    new PublicLoot(
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot(
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier3),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier3),
                        Tier.Accessory(Loot.Chance(3), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot(
                        Tier.Weapon(Loot.Chance(25), ItemTier.Tier3),
                        Tier.Armor(Loot.Chance(15), ItemTier.Tier3),
                        Tier.Accessory(Loot.Chance(3), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot(
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier3),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier3),
                        Tier.Weapon(Loot.Chance(50), ItemTier.Tier4),
                        Tier.Armor(Loot.Chance(35), ItemTier.Tier4),
                        Tier.Accessory(Loot.Chance(15), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new SoulboundLoot(0.2f, // 4
                        new Single(Loot.Chance(8), new Item("Firefly"))
                        ),
                } },

                { SoulGroup.RictornsGate, new List<LootContainer>
                {
                    new PublicLoot(
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot(
                        Tier.Weapon(Loot.Chance(20), ItemTier.Tier3),
                        Tier.Armor(Loot.Chance(10), ItemTier.Tier3),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier2)
                        ),
                    new PublicLoot(
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier4),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier4),
                        Tier.Accessory(Loot.Chance(15), ItemTier.Tier2)
                        ),
                    new SoulboundLoot(0.2f, // 3
                        new Single(Loot.Chance(10), new Item("Ceremonial Bow"))
                        ),
                    new SoulboundLoot(1, // 4
                        new Single(Loot.Chance(2), new Item("Ceremonial Bow"))
                        ),
                    new SoulboundLoot(0.2f, // 5
                        new Single(Loot.Chance(15), new Item("Chestplate of the Forest"))
                        ),
                    new SoulboundLoot(1, // 6
                        new Single(Loot.Chance(2), new Item("Chestplate of the Forest"))
                        ),
                    new SoulboundLoot(0.4f, // 7
                        new Single(Loot.Chance(40),
                            new Item("Scroll of Agility")
                        )
                        ),
                    new SoulboundLoot(0.4f, // 8
                        new Single(Loot.Chance(20),
                            new Item("Scroll of Fortitude")
                        )
                        ),
                    new SoulboundLoot(0.4f, // 9
                        new Single(Loot.Chance(40),
                            new Item("Scroll of Stamina")
                        )
                        ),
                    new SoulboundLoot(1, // 10
                        new Single(Loot.Chance(2), new Item("Corrupted Skull of Mezhier"))
                        ),
                    new SoulboundLoot(1, // 11
                        new Single(Loot.Chance(25), new Item("Key to Whispering Woods"))
                        ),
                } },

                { SoulGroup.Desert, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 1
                        Tier.Weapon(Loot.Chance(6), ItemTier.Tier4),
                        Tier.Armor(Loot.Chance(5), ItemTier.Tier4),
                        Tier.Accessory(Loot.Chance(3), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 2
                        Tier.Weapon(Loot.Chance(20), ItemTier.Tier4),
                        Tier.Armor(Loot.Chance(10), ItemTier.Tier4),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier5),
                        Tier.Accessory(Loot.Chance(10), ItemTier.Tier2)
                        ),
                    new SoulboundLoot(0.2f, // 4
                        new Single(Loot.Chance(2),
                            new Item("Scroll of Agility"))
                        ),
                    new SoulboundLoot(0.2f, // 5
                        new Single(Loot.Chance(5),
                            new Item("Windward Cloak"))
                        ),
                    new SoulboundLoot(0.2f, // 6
                        new Single(Loot.Chance(100),
                            new Item("Scroll of Agility"))
                        ),
                    new SoulboundLoot(0.3f, // 7
                        new Single(Loot.Chance(10),
                            new Item("Quartz Ring"))
                        ),
                } },

                { SoulGroup.Gorge, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(15), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 1
                        Tier.Weapon(Loot.Chance(6), ItemTier.Tier4),
                        Tier.Armor(Loot.Chance(5), ItemTier.Tier4),
                        Tier.Accessory(Loot.Chance(3), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 2
                        Tier.Weapon(Loot.Chance(20), ItemTier.Tier4),
                        Tier.Armor(Loot.Chance(10), ItemTier.Tier4),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier2),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier5),
                        Tier.Accessory(Loot.Chance(10), ItemTier.Tier2)
                        ),
                    new SoulboundLoot(0.2f, // 4
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier6),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier5),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier3)
                        ),
                    new SoulboundLoot(0.2f, // 5
                        new Single(Loot.Chance(100),
                            new Item("Scroll of Agility")
                        )
                        ),
                    new SoulboundLoot(0.2f, // 6
                        new Single(Loot.Chance(2),
                            new Item("Scroll of Agility")
                        )
                        ),
                    new SoulboundLoot(0.2f, // 7
                        new Single(Loot.Chance(15),
                            new Item("Scroll of Power")
                        )
                        ),
                    new SoulboundLoot(0.2f, // 8
                        new Single(Loot.Chance(15),
                            new Item("Scroll of Fortitude")
                        )
                        ),
                    new SoulboundLoot(0.2f, // 9
                        new Single(Loot.Chance(15),
                            new Item("Scroll of Stamina")
                        )
                        ),
                    new SoulboundLoot(0.2f, // 10
                        new Single(Loot.Chance(1),
                            new Item("Scroll of Power")
                        )
                        ),
                    new SoulboundLoot(0.3f, // 11
                        new Single(Loot.Chance(0.8),
                            new Item("Desert Rose"))
                        ),
                    new SoulboundLoot(0.3f, // 12
                        new Single(Loot.Chance(10),
                            new Item("Desert Rose"))
                        ),
                    new SoulboundLoot(0.3f, // 13
                        new Single(Loot.Chance(25),
                            new Item("Key to Bubra Barrens"))
                        ),
                } },

                { SoulGroup.Lake, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 1
                        Tier.Weapon(Loot.Chance(6), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(5), ItemTier.Tier5),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 2
                        Tier.Weapon(Loot.Chance(20), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(10), ItemTier.Tier5),
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new PublicLoot( // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier5)
                        ),
                    new SoulboundLoot(0.2f, // 4
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier6),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier5),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier5),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier3),
                        new Single(Loot.Chance(20),
                            new Item("Scroll of Power"))
                        ),
                    new SoulboundLoot(0.3f, // 5
                        new Single(Loot.Chance(2),
                            new Item("Scroll of Power"))
                        ),
                    new SoulboundLoot(0.3f, // 6
                        new Single(Loot.Chance(2.5),
                            new Item("Band of Oceanic Radiance"))
                        ),
                    new SoulboundLoot(0.3f, // 7
                        new Single(Loot.Chance(0.6),
                            new Item("Seafarer's Garb"))
                        ),
                    new SoulboundLoot(0.3f, // 8
                        new Single(Loot.Chance(0.6),
                            new Item("Salt Water Elixir"))
                        ),
                    new SoulboundLoot(0.3f, // 8
                        new Single(Loot.Chance(3),
                            new Item("Salt Water Elixir"))
                        ),
                } },

                { SoulGroup.Tundra, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(10), new Item("Healing Spell"))
                        ),
                    new SoulboundLoot( // 1
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier6),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier3)
                        ),
                    new SoulboundLoot( // 2
                        Tier.Weapon(Loot.Chance(35), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(20), ItemTier.Tier6),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier3)
                        ),
                    new SoulboundLoot(0.2f, // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier7),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier6),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier3),
                        new Single(Loot.Chance(100),
                            new Item("Scroll of Power"))
                        ),
                    new SoulboundLoot(0.3f, // 4
                        new Single(Loot.Chance(4),
                            new Item("Scroll of Stamina"))
                        ),
                    new SoulboundLoot(0.1f, // 5
                        new Single(Loot.Chance(5), new Item("Dumirian Kilt"))
                        ),
                    new SoulboundLoot(0.1f, // 6
                        new Single(Loot.Chance(0.5), new Item("Dumirian Kilt"))
                        ),
                } },

                { SoulGroup.Mountains, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(10), new Item("Healing Spell")),
                        new Single(Loot.Chance(5), new Item("Greater Healing Spell"))
                        ),
                    new SoulboundLoot( // 1
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier6),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier3)
                        ),
                    new SoulboundLoot( // 2
                        Tier.Weapon(Loot.Chance(35), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(20), ItemTier.Tier6),
                        Tier.Accessory(Loot.Chance(5), ItemTier.Tier3)
                        ),
                    new SoulboundLoot(0.2f, // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier7),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier6),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier3)
                        ),
                    new SoulboundLoot(0.3f, // 4
                        new Single(Loot.Chance(100), 
                            new Item("Scroll of Agility"),
                            new Item("Scroll of Power"),
                            new Item("Scroll of Fortitude"),
                            new Item("Scroll of Stamina"))
                        ),
                    new SoulboundLoot(0.3f, // 5
                        new Single(Loot.Chance(8),
                            new Item("Scroll of Fortitude"))
                        ),
                    new SoulboundLoot(0.1f, // 6
                        new Single(Loot.Chance(10), new Item("Cloak of Doom"))
                        ),
                    new SoulboundLoot(0.1f, // 7
                        new Single(Loot.Chance(0.5), new Item("Cloak of Doom"))
                        ),
                } },

                { SoulGroup.ValdoksForge, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(15), new Item("Healing Spell")),
                        new Single(Loot.Chance(7.5), new Item("Greater Healing Spell"))
                        ),
                    new SoulboundLoot( // 1
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier6)
                        ),
                    new SoulboundLoot( // 2
                        Tier.Weapon(Loot.Chance(35), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(20), ItemTier.Tier6)
                        ),
                    new SoulboundLoot(0.1f, // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier7),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier6),
                        //Tier.Weapon(Loot.Chance(6), ItemTier.Tier8),
                        //Tier.Armor(Loot.Chance(4), ItemTier.Tier8),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier3)
                        ),
                    new SoulboundLoot(0.1f, // 4
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier8),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier8),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier7),
                        //Tier.Weapon(Loot.Chance(6), ItemTier.Tier9),
                        //Tier.Armor(Loot.Chance(4), ItemTier.Tier9),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier4)
                        ),
                    new SoulboundLoot(0.4f, // 5
                        new Single(Loot.Chance(80), new Item("Scroll of Fortitude"))
                        ),
                    new SoulboundLoot(0.8f, // 6
                        new Single(Loot.Chance(80), new Item("Scroll of Fortitude"))
                        ),
                    new SoulboundLoot(0.1f, // 7
                        new Single(Loot.Chance(2), new Item("Bothmur's Zweihander"))
                        ),
                    new SoulboundLoot(0.1f, // 8
                        new Single(Loot.Chance(2), new Item("Valdok's Impervious Aegis"))
                        new Single(Loot.Chance(1.5), new Item("Lifesource Chassis"))
                        ),
                    new SoulboundLoot(0.1f, // 9 mythic accessories
                        new Single(Loot.Chance(0.9), new Item("Tehtman's Brutal Band")),
                        new Single(Loot.Chance(0.9), new Item("Ring of Stalwart Vitality")),
                        new Single(Loot.Chance(0.9), new Item("Aldrite's Gamble")),
                        new Single(Loot.Chance(0.9), new Item("Skyjewel")),
                        new Single(Loot.Chance(0.9), new Item("Studded Bangle"))
                        ),
                    new SoulboundLoot(0.1f, // 10 shared mythic
                        new Single(Loot.Chance(0.5), new Item("Shining Armor")),
                        new Single(Loot.Chance(0.5), new Item("Plague Hide"))
                        ),
                    new SoulboundLoot(0.6f, // 11
                        new Single(Loot.Chance(80), new Item("Scroll of Fortitude"))
                        ),
                    new SoulboundLoot(0.6f, // 12
                        new Single(Loot.Chance(25), new Item("Key to Valdok's Forge"))
                        ),
                    new SoulboundLoot(0.6f, // 13
                        new Single(Loot.Chance(15), new Item("Key to Dumir"))
                        ),
                    new SoulboundLoot(0.1f, // 14
                        new Single(Loot.Chance(15),
                            new Item("Talisman of Blood"),
                            new Item("Talisman of Nova"),
                            new Item("Talisman of Millennia"),
                            new Item("Symbiotic Talisman"),
                            new Item("Talisman of Tragedy"),
                            new Item("Ethereal Talisman"),
                            new Item("Talisman of Strength"),
                            new Item("Talisman of the Shield"),
                            new Item("Talisman of Piercing"),
                            new Item("Talisman of Celerity"))
                        ),
                    new SoulboundLoot(0.1f, // 15
                        new Single(Loot.Chance(15),
                            new Item("Talisman of Ages"),
                            new Item("Talisman of Wrath"),
                            new Item("Blessed Talisman"),
                            new Item("Rogue's Talisman"),
                            new Item("Talisman of Daybreak"),
                            new Item("Soothing Talisman"),
                            new Item("Talisman of Holy Water"),
                            new Item("Talisman of Retribution"),
                            new Item("Talisman of Spite"),
                            new Item("Talisman of Apocalypse"))
                        ),
                } },

                { SoulGroup.Dumir, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(15), new Item("Healing Spell")),
                        new Single(Loot.Chance(7.5), new Item("Greater Healing Spell"))
                        ),
                    new SoulboundLoot( // 1
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier6)
                        ),
                    new SoulboundLoot( // 2
                        Tier.Weapon(Loot.Chance(35), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(20), ItemTier.Tier6)
                        ),
                    new SoulboundLoot(0.1f, // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier7),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier6),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier6),
                        //Tier.Weapon(Loot.Chance(6), ItemTier.Tier8),
                        //Tier.Armor(Loot.Chance(4), ItemTier.Tier8),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier4)
                        ),
                    new SoulboundLoot(0.1f, // 4
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier8),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier8),
                        Tier.Weapon(Loot.Chance(55), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(40), ItemTier.Tier7),
                        //Tier.Weapon(Loot.Chance(6), ItemTier.Tier9),
                        //Tier.Armor(Loot.Chance(4), ItemTier.Tier9),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier4)
                        ),
                    new SoulboundLoot(0.3f, // 5
                        new Single(Loot.Chance(70), new Item("Scroll of Stamina", false, 1))
                        ),
                    new SoulboundLoot(0.3f, // 6
                        new Single(Loot.Chance(70), new Item("Scroll of Stamina"))
                        ),
                    new SoulboundLoot(0.1f, // 7
                        new Single(Loot.Chance(2), new Item("Oda's Transcendent Longbow"))
                        ),
                    new SoulboundLoot(0.1f, // 8
                        new Single(Loot.Chance(2), new Item("Archmage's Sibylline Vestment"))
                        new Single(Loot.Chance(1.5), new Item("Lifesource Chassis"))
                        ),
                    new SoulboundLoot(0.1f, // 9 mythic accessories
                        new Single(Loot.Chance(0.25), new Item("Arcus's Nimble Circlet")),
                        new Single(Loot.Chance(0.25), new Item("Adorned Band")),
                        new Single(Loot.Chance(0.25), new Item("Ring of the Lonely Spirit")),
                        new Single(Loot.Chance(0.25), new Item("Aldrite's Gamble")),
                        new Single(Loot.Chance(0.25), new Item("Skyjewel")),
                        new Single(Loot.Chance(0.25), new Item("Studded Bangle"))
                        ),
                    new SoulboundLoot(0.1f, // 10 shared mythic
                        new Single(Loot.Chance(0.5), new Item("Shining Armor")),
                        new Single(Loot.Chance(0.5), new Item("Plague Hide"))
                        ),
                    new SoulboundLoot(0.1f, // 11
                        new Single(Loot.Chance(25), new Item("Key to Dumir"))
                        ),
                    new SoulboundLoot(0.1f, // 12
                        new Single(Loot.Chance(15), new Item("Key to Valdok's Forge"))
                        ),
                    new SoulboundLoot(0.1f, // 13
                        new Single(Loot.Chance(15),
                            new Item("Talisman of Blood"),
                            new Item("Talisman of Nova"),
                            new Item("Talisman of Millennia"),
                            new Item("Symbiotic Talisman"),
                            new Item("Talisman of Tragedy"),
                            new Item("Ethereal Talisman"),
                            new Item("Talisman of Strength"),
                            new Item("Talisman of the Shield"),
                            new Item("Talisman of Piercing"),
                            new Item("Talisman of Celerity"))
                        ),
                    new SoulboundLoot(0.1f, // 14
                        new Single(Loot.Chance(15),
                            new Item("Talisman of Ages"),
                            new Item("Talisman of Wrath"),
                            new Item("Blessed Talisman"),
                            new Item("Rogue's Talisman"),
                            new Item("Talisman of Daybreak"),
                            new Item("Soothing Talisman"),
                            new Item("Talisman of Holy Water"),
                            new Item("Talisman of Retribution"),
                            new Item("Talisman of Spite"),
                            new Item("Talisman of Apocalypse"))
                        ),
                    new SoulboundLoot(0.1f, // 15
                        new Single(Loot.Chance(2), new Item("Thumbor"))
                        ),
                    new SoulboundLoot(0.1f, // 16
                        new Single(Loot.Chance(2), new Item("Raeg's Ethereal Spear"))
                        ),
                    new SoulboundLoot(0.1f, // 17
                        new Single(Loot.Chance(2), new Item("Ring of Sinful Beauty"))
                        ),
                } },

                { SoulGroup.MannahsFortress, new List<LootContainer>
                {
                    new PublicLoot( // 0
                        new Single(Loot.Chance(20), new Item("Healing Spell")),
                        new Single(Loot.Chance(10), new Item("Greater Healing Spell"))
                        ), 
                    new SoulboundLoot( // 1
                        Tier.Weapon(Loot.Chance(10), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(8), ItemTier.Tier7)
                        ),
                    new SoulboundLoot( // 2
                        Tier.Weapon(Loot.Chance(25), ItemTier.Tier7),
                        Tier.Armor(Loot.Chance(15), ItemTier.Tier7)
                        ),
                    new SoulboundLoot(0.2f, // 3
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier8),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier8),
                        Tier.Accessory(Loot.Chance(20), ItemTier.Tier4)
                        ),
                    new SoulboundLoot(0.1f, // 4
                        Tier.Weapon(Loot.Chance(70), ItemTier.Tier8),
                        Tier.Armor(Loot.Chance(50), ItemTier.Tier8),
                        Tier.Weapon(Loot.Chance(40), ItemTier.Tier9),
                        Tier.Armor(Loot.Chance(30), ItemTier.Tier9),
                        Tier.Weapon(Loot.Chance(12), ItemTier.Tier10),
                        //Tier.Armor(Loot.Chance(3), ItemTier.Tier10),
                        Tier.Accessory(Loot.Chance(8), ItemTier.Tier5)
                        ),
                    new SoulboundLoot(0.1f, // 5
                        new Single(Loot.Chance(100),
                            new Item("Scroll of Agility"),
                            new Item("Scroll of Power"),
                            new Item("Scroll of Fortitude"),
                            new Item("Scroll of Stamina"))
                        ),
                    new SoulboundLoot(0.1f, // 6
                        new Single(Loot.Chance(8),
                            new Item("Scroll of Agility"),
                            new Item("Scroll of Power"),
                            new Item("Scroll of Fortitude"),
                            new Item("Scroll of Stamina"))
                        ),
                    new SoulboundLoot(0.1f, // 7 mythic weapons
                        new Single(Loot.Chance(1.4), new Item("Mannah's Soul Crux")),
                        new Single(Loot.Chance(1.4), new Item("Mannah's Mop")),
                        new Single(Loot.Chance(1.4), new Item("Stormsong (Tehtman's Soulstring)")),
                        new Single(Loot.Chance(1.4), new Item("Elixir of Enigma")),
                        new Single(Loot.Chance(1.4), new Item("Sword of Unknown"))
                        ),
                    new SoulboundLoot(0.1f, // 8 mythic armor
                        new Single(Loot.Chance(0.7), new Item("Empyrean's Guard"))
                        ),
                    new SoulboundLoot(0.1f, // 9 mythic accessories
                        new Single(Loot.Chance(0.8), new Item("Mezhier's Ring of Valor")),
                        new Single(Loot.Chance(0.8), new Item("Aldrite's Gamble")),
                        new Single(Loot.Chance(0.8), new Item("Skyjewel")),
                        new Single(Loot.Chance(0.8), new Item("Ring of the Blood Moon"))
                        ),
                    new SoulboundLoot(0.1f, // 10 fabled
                        new Single(Loot.Chance(0.4), new Item("Dark Matter")),
                        new Single(Loot.Chance(0.4), new Item("The Big Bang")),
                        new Single(Loot.Chance(0.4), new Item("Sadder Star")),
                        new Single(Loot.Chance(0.4), new Item("The Malevolant Eye"))
                        ),
                    new SoulboundLoot(0.1f, // 11
                        new Single(Loot.Chance(30), new Item("Key to Mannah's Fortress"))
                        ),
                    new SoulboundLoot(0.1f, // 12
                        new Single(Loot.Chance(20), new Item("Key to Dumir"))
                        ),
                    new SoulboundLoot(0.1f, // 13
                        new Single(Loot.Chance(20), new Item("Key to Valdok's Forge"))
                        ),
                    new SoulboundLoot(0.1f, // 14
                        new Single(Loot.Chance(100),
                            new Item("Scroll of Life"))
                        ),
                    new SoulboundLoot(0.1f, // 15
                        new Single(Loot.Chance(15),
                            new Item("Talisman of Blood"),
                            new Item("Talisman of Nova"),
                            new Item("Talisman of Millennia"),
                            new Item("Symbiotic Talisman"),
                            new Item("Talisman of Tragedy"),
                            new Item("Ethereal Talisman"),
                            new Item("Talisman of Strength"),
                            new Item("Talisman of the Shield"),
                            new Item("Talisman of Piercing"),
                            new Item("Talisman of Celerity"))
                        ),
                    new SoulboundLoot(0.1f, // 16
                        new Single(Loot.Chance(15),
                            new Item("Talisman of Ages"),
                            new Item("Talisman of Wrath"),
                            new Item("Blessed Talisman"),
                            new Item("Rogue's Talisman"),
                            new Item("Talisman of Daybreak"),
                            new Item("Soothing Talisman"),
                            new Item("Talisman of Holy Water"),
                            new Item("Talisman of Retribution"),
                            new Item("Talisman of Spite"),
                            new Item("Talisman of Apocalypse"))
                        ),
                    new SoulboundLoot(0.1f, // 17
                        new Single(Loot.Chance(2), new Item("Mannah's Capstone"))
                        ),
                } },
            };
        }

        public static List<LootContainer> GetLootContainers(EnemyInfo info)
        {
            var containers = new List<LootContainer>();
            if (info.lootTiers.Length == 0) return containers;
            foreach (var lootTier in info.lootTiers)
            {
                if (!lootTables.TryGetValue(info.soulGroup, out var table))
                    continue;
                if (lootTier >= table.Count)
                    continue;
                containers.Add(table[lootTier]);
            }
            return containers;
        }
    }
}
