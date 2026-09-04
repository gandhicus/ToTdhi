using System;
using System.Globalization;
using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class ProcStatBonus
    {
        public StatType statType;

        public int amount;

        public uint durationMs;

        public ProcStatBonus(StatType statType, int amount, uint durationMs)
        {
            this.statType = statType;
            this.amount = amount;
            this.durationMs = durationMs;
        }

        public ProcStatBonus(XmlParser xml)
        {
            statType = xml.AtrEnum("type", StatType.Vigor);
            amount = xml.AtrInt("amount", 0);
            durationMs = (uint)xml.AtrInt("duration", 0);
        }
    }

    public class ProcAlternateStatBonus
    {
        public AlternateStatType statType;

        public int amount;

        public uint durationMs;

        public ProcAlternateStatBonus(AlternateStatType statType, int amount, uint durationMs)
        {
            this.statType = statType;
            this.amount = amount;
            this.durationMs = durationMs;
        }

        public ProcAlternateStatBonus(XmlParser xml)
        {
            statType = xml.AtrEnum("type", AlternateStatType.RateOfFire);
            amount = xml.AtrInt("amount", 0);
            durationMs = (uint)xml.AtrInt("duration", 0);
        }
    }

    public class ProcRageGain
    {
        public float amount;

        public ProcRageGain(XmlParser xml)
        {
            if (xml.TryGetAttribute("amount", out var amountAttr))
                amount = Convert.ToSingle(amountAttr.Value, CultureInfo.InvariantCulture);
            else if (!string.IsNullOrWhiteSpace(xml.stringValue))
                amount = xml.intValue;
        }
    }

    public class ItemProc
    {
        public ProcTrigger trigger;

        public uint cooldownMs;

        public ProcStatBonus statBonus;

        public ProcAlternateStatBonus alternateStatBonus;

        public ProcRageGain rageGain;

        public TalismanAoe aoe;

        public ItemProc(XmlParser xml)
        {
            trigger = xml.AtrEnum("trigger", ProcTrigger.CriticalStrike);
            cooldownMs = (uint)xml.AtrInt("cooldown", 0);

            foreach (var child in xml.Elements("StatBonus"))
                statBonus = new ProcStatBonus(child);

            foreach (var child in xml.Elements("AlternateStatBonus"))
                alternateStatBonus = new ProcAlternateStatBonus(child);

            foreach (var child in xml.Elements("Rage"))
                rageGain = new ProcRageGain(child);

            foreach (var child in xml.Elements("Aoe"))
                aoe = new TalismanAoe(child);
        }
    }
}
