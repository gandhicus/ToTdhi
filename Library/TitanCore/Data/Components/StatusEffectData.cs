using System;
using System.Globalization;
using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class StatusEffectData
    {
        /// <summary>
        /// The type of status effect to apply
        /// </summary>
        public StatusEffect type;

        /// <summary>
        /// Optional magnitude (e.g. DefenseMinus amount), same pattern as StatBonus amount.
        /// </summary>
        public int amount;

        /// <summary>
        /// The duration of the status effect
        /// </summary>
        public uint duration;

        public StatusEffectData(XmlParser xml)
        {
            Parse(xml);
        }

        /// <summary>
        /// Parses xml data
        /// </summary>
        /// <param name="xml"></param>
        public void Parse(XmlParser xml)
        {
            type = xml.AtrEnum("type", StatusEffect.Slowed);
            amount = xml.AtrInt("amount", 0);
            duration = (uint)xml.AtrInt("duration", 0);
            if (duration == 0)
            {
                var text = xml.stringValue;
                if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                    duration = (uint)parsed;
            }
        }
    }
}
