using System;
using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data.Components;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Items
{
    public class EquipmentInfo : ItemInfo
    {
        public override GameObjectType Type => GameObjectType.Equipment;

        public Dictionary<StatType, int> statIncreases = new Dictionary<StatType, int>();

        public Dictionary<AlternateStatType, int> alternateStatIncreases = new Dictionary<AlternateStatType, int>();

        public ItemTier tier = ItemTier.Untiered;

        public bool soulless = false;

        public StatRollPool statRollPool;

        public List<ItemProc> procs = new List<ItemProc>();

        public List<ScaledStatIncrease> scaledStatIncreases = new List<ScaledStatIncrease>();

        public List<TalentRankIncrease> talentRanks = new List<TalentRankIncrease>();

        public ClassType requiredClass;

        public List<TalismanEffect> talismanEffects = new List<TalismanEffect>();

        public List<EffectStyle> effectStyles = new List<EffectStyle>();

        public EquipmentInfo() : base()
        {

        }

        public override void Parse(XmlParser xml)
        {
            base.Parse(xml);

            if (Enum.TryParse<ItemTier>(xml.AtrString("tier", "-1"), true, out var result))
                tier = result;

            foreach (var statIncrease in xml.Elements("StatIncrease").Select(_ => new StatIncrease(_)))
            {
                if (!statIncreases.TryGetValue(statIncrease.type, out var amount))
                    amount = 0;
                amount += statIncrease.amount;
                statIncreases[statIncrease.type] = amount;
            }

            foreach (var increase in xml.Elements("AlternateStatIncrease").Select(_ => new AlternateStatIncrease(_)))
            {
                if (!alternateStatIncreases.TryGetValue(increase.type, out var amount))
                    amount = 0;
                amount += increase.amount;
                alternateStatIncreases[increase.type] = amount;
            }

            soulless = xml.Exists("Soulless");

            if (xml.TryGetValue("StatRolls", out var statRollsElement))
                statRollPool = new StatRollPool(new XmlParser(statRollsElement));

            foreach (var proc in xml.Elements("Proc"))
                procs.Add(new ItemProc(proc));

            foreach (var scaled in xml.Elements("ScaledStatIncrease"))
                scaledStatIncreases.Add(new ScaledStatIncrease(scaled));

            foreach (var rank in xml.Elements("TalentRank"))
                talentRanks.Add(new TalentRankIncrease(rank));

            var className = xml.String("Class", "");
            if (!string.IsNullOrEmpty(className) && Enum.TryParse<ClassType>(className, true, out var parsedClass))
                requiredClass = parsedClass;

            foreach (var effectXml in xml.Elements("TalismanEffect"))
                talismanEffects.Add(new TalismanEffect(effectXml));

            var legacySpecial = xml.String("TalismanSpecial", "");
            if (talismanEffects.Count == 0 && string.Equals(legacySpecial, "DefensePulse", StringComparison.OrdinalIgnoreCase))
                talismanEffects.Add(TalismanEffect.CreateDefensePulse());

            EffectStyleFunctions.ParseXml(xml, effectStyles);
        }

        public string GetTierDisplay()
        {
            switch (tier)
            {
                case ItemTier.Untiered:
                    return "UT";
                case ItemTier.Starter:
                    return "S";
                default:
                    return "T" + (int)tier;
            }
        }
    }
}
