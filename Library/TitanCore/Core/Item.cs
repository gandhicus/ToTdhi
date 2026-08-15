using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TitanCore.Data;
using TitanCore.Data.Items;
using Utils.NET.IO;
using Utils.NET.Logging;

namespace TitanCore.Core
{
    public enum ItemEnchantType
    {
        None = 0,
        Damaging = 1
    }

    public struct Item
    {
        public static Item Blank = new Item(0);

        public static Item ReadItem(BitReader r)
        {
            var item = new Item();
            item.Read(r);
            return item;
        }

        /// <summary>
        /// The id of this item
        /// </summary>
        public ushort id;

        /// <summary>
        /// Determines if this item is soulbound
        /// </summary>
        public bool soulbound;

        /// <summary>
        /// The amount of items this contains
        /// </summary>
        public byte count;

        /// <summary>
        /// The type of enchantment on this item
        /// </summary>
        public ItemEnchantType enchantType;

        /// <summary>
        /// The level of the enchantment
        /// </summary>
        public byte enchantLevel;

        /// <summary>
        /// Rolled primary stat increases (for items with random stat pools)
        /// </summary>
        [XmlIgnore]
        public Dictionary<StatType, int> rolledStatIncreases;

        /// <summary>
        /// Rolled alternate stat increases (for items with random stat pools)
        /// </summary>
        [XmlIgnore]
        public Dictionary<AlternateStatType, int> rolledAlternateStatIncreases;

        /// <summary>
        /// The cached info of this item
        /// </summary>
        private ItemInfo info;

        public bool IsBlank => id == 0;

        public Item(ushort id)
        {
            this.id = id;
            soulbound = false;
            count = 1;
            enchantType = ItemEnchantType.None;
            enchantLevel = 0;
            rolledStatIncreases = null;
            rolledAlternateStatIncreases = null;

            info = null;
        }

        public Item(ushort id, bool soulbound)
        {
            this.id = id;
            this.soulbound = soulbound;
            count = 1;
            enchantType = ItemEnchantType.None;
            enchantLevel = 0;
            rolledStatIncreases = null;
            rolledAlternateStatIncreases = null;

            info = null;
        }

        public Item(ushort id, byte count)
        {
            this.id = id;
            soulbound = false;
            this.count = count;
            enchantType = 0;
            enchantLevel = 0;
            rolledStatIncreases = null;
            rolledAlternateStatIncreases = null;

            info = null;
        }

        public Item(ushort id, bool soulbound, byte count)
        {
            this.id = id;
            this.soulbound = soulbound;
            this.count = count;
            enchantType = ItemEnchantType.None;
            enchantLevel = 0;
            rolledStatIncreases = null;
            rolledAlternateStatIncreases = null;

            info = null;
        }

        public Item(string name, bool soulbound = false, byte count = 1)
        {
            var objectInfo = GameData.GetObjectByName(name);
            if (objectInfo == null)
                throw new ArgumentException($"Unknown item name: '{name}'", nameof(name));
            if (!(objectInfo is ItemInfo itemInfo))
                throw new ArgumentException($"Object '{name}' is not an item.", nameof(name));

            info = itemInfo;
            id = info.id;
            this.soulbound = soulbound;
            this.count = count;
            enchantType = ItemEnchantType.None;
            enchantLevel = 0;
            rolledStatIncreases = null;
            rolledAlternateStatIncreases = null;
        }

        public void Read(BitReader r)
        {
            id = r.ReadUInt16();
            if (id == 0) return;
            soulbound = r.ReadBool();
            count = (byte)r.Read(7);
            if (r.ReadBool()) // has augments
            {
                enchantType = (ItemEnchantType)r.ReadUInt8();
                enchantLevel = r.ReadUInt8();
            }
            if (r.ReadBool()) // has rolled stats
            {
                rolledStatIncreases = new Dictionary<StatType, int>();
                var statCount = r.ReadUInt8();
                for (int i = 0; i < statCount; i++)
                {
                    var type = (StatType)r.ReadUInt8();
                    var amount = r.ReadInt16();
                    rolledStatIncreases[type] = amount;
                }

                rolledAlternateStatIncreases = new Dictionary<AlternateStatType, int>();
                var altStatCount = r.ReadUInt8();
                for (int i = 0; i < altStatCount; i++)
                {
                    var type = (AlternateStatType)r.ReadUInt8();
                    var amount = r.ReadInt16();
                    rolledAlternateStatIncreases[type] = amount;
                }
            }
            else
            {
                rolledStatIncreases = null;
                rolledAlternateStatIncreases = null;
            }
        }

        public void Write(BitWriter w)
        {
            w.Write(id);
            if (id == 0) return;
            w.Write(soulbound);
            w.Write(count, 7);
            if (enchantType != ItemEnchantType.None) // no augments
            {
                w.Write(true);
                w.Write((byte)enchantType);
                w.Write(enchantLevel);
            }
            else
                w.Write(false);

            var hasRolledStats = EquipmentStatFunctions.HasRolledStats(this);
            w.Write(hasRolledStats);
            if (hasRolledStats)
            {
                w.Write((byte)rolledStatIncreases.Count);
                foreach (var increase in rolledStatIncreases)
                {
                    w.Write((byte)increase.Key);
                    w.Write((short)increase.Value);
                }

                w.Write((byte)rolledAlternateStatIncreases.Count);
                foreach (var increase in rolledAlternateStatIncreases)
                {
                    w.Write((byte)increase.Key);
                    w.Write((short)increase.Value);
                }
            }
        }

        public bool CanSwapInto(SlotType slotType)
        {
            if (IsBlank) return true;
            if (slotType == SlotType.Generic) return true;
            return slotType == GetInfo().slotType;
        }

        public bool CanSwapInto(SlotType slotType, Data.Entities.CharacterInfo charInfo, int slotIndex)
        {
            if (IsBlank) return true;
            if (slotType == SlotType.Generic) return true;
            if (charInfo != null)
                return charInfo.CanEquipInSlot(GetInfo().slotType, slotIndex);
            return CanSwapInto(slotType);
        }

        public ItemInfo GetInfo()
        {
            return info ?? (info = (ItemInfo)GameData.objects[id]);
        }

        public SlotType GetSlotType()
        {
            if (IsBlank) return SlotType.Generic;
            return GetInfo().slotType;
        }

        public byte[] ToBinary()
        {
            var w = new BitWriter();
            Write(w);
            return w.GetData().data;
        }

        public static Item FromBinary(byte[] bytes)
        {
            var r = new BitReader(bytes, bytes.Length);
            return ReadItem(r);
        }

        public override bool Equals(object obj)
        {
            if (obj is Item item)
                return this == item;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator==(Item a, Item b)
        {
            return a.id == b.id && a.soulbound == b.soulbound && a.count == b.count && a.enchantType == b.enchantType && a.enchantLevel == b.enchantLevel && EquipmentStatFunctions.RolledStatsEqual(a, b);
        }

        public static bool operator !=(Item a, Item b)
        {
            return !(a == b);
        }
    }
}
