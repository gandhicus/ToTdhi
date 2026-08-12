using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TitanCore.Core;
using TitanCore.Data.Components;
using Utils.NET.IO.Xml;
using Utils.NET.Utils;

namespace TitanCore.Data.Entities
{
    public class CharacterInfo : EntityInfo
    {
        private static readonly Dictionary<string, SlotType[]> AdditionalArmorTypes = new Dictionary<string, SlotType[]>
        {
            { "Ranger", new[] { SlotType.Robe } },
            { "Lancer", new[] { SlotType.Robe } },
            { "Nomad", new[] { SlotType.Robe } },
            { "Commander", new[] { SlotType.LightArmor } },
            { "Warrior", new[] { SlotType.LightArmor } },
            { "Berserker", new[] { SlotType.LightArmor } },
            { "Alchemist", new[] { SlotType.LightArmor } },
            { "Minister", new[] { SlotType.LightArmor } },
            { "Brewer", new[] { SlotType.LightArmor } },
        };

        public override GameObjectType Type => GameObjectType.Character;

        /// <summary>
        /// The stat info of the character
        /// </summary>
        public Dictionary<StatType, StatData> stats;

        public SlotType[] equipSlots;

        public ushort[] defaultItems;

        public ClassRequirement[] requirements;

        public int displayOrder = 0;

        public bool notPlayable;

        public CharacterInfo() : base()
        {

        }

        public override void Parse(XmlParser xml)
        {
            base.Parse(xml);

            stats = xml.Elements("Stat").Select(_ => new StatData(_)).ToDictionary(_ => _.type);

            var slotType = typeof(SlotType);
            equipSlots = xml.String("Slots", "Bow,LightArmor,Accessory,Accessory").Split(',').Select(_ => (SlotType)Enum.Parse(slotType, _.Trim())).ToArray();

            defaultItems = xml.String("DefaultItems", "0x0").Split(',').Select(_ => (ushort)StringUtils.ParseHex(_)).ToArray();

            requirements = xml.Elements("ClassRequirement").Select(_ => new ClassRequirement(_)).ToArray();

            displayOrder = xml.Int("DisplayOrder", id);
            notPlayable = xml.Exists("NotPlayable");
        }

        public static bool IsArmorSlotType(SlotType slotType)
        {
            return slotType == SlotType.LightArmor || slotType == SlotType.HeavyArmor || slotType == SlotType.Robe;
        }

        public bool CanUseSloType(SlotType slotType)
        {
            for (int i = 0; i < equipSlots.Length; i++)
                if (equipSlots[i] == slotType)
                    return true;

            if (AdditionalArmorTypes.TryGetValue(name, out var extras))
            {
                for (int i = 0; i < extras.Length; i++)
                    if (extras[i] == slotType)
                        return true;
            }

            return false;
        }

        public bool CanEquipInSlot(SlotType itemSlotType, int slotIndex)
        {
            if (slotIndex >= equipSlots.Length)
                return itemSlotType == SlotType.Generic;

            if (slotIndex == 1 && IsArmorSlotType(itemSlotType))
                return CanUseSloType(itemSlotType);

            return equipSlots[slotIndex] == itemSlotType;
        }
    }
}
