using System;
using System.Collections.Generic;
using Utils.NET.IO.Xml;

namespace TitanCore.Core
{
    public enum EffectStyle : byte
    {
        Power = 0,
        Agility = 1,
        Focus = 2,
        Defense = 3,
        Support = 4
    }

    public static class EffectStyleFunctions
    {
        public const string Power_Hex = "A75AF1";
        public const string Agility_Hex = "28DB3F";
        public const string Focus_Hex = "E87E07";
        public const string Support_Hex = "DE023C";
        public const string Defense_Hex = "9BA0B4";

        public static string GetHex(EffectStyle style)
        {
            switch (style)
            {
                case EffectStyle.Power:
                    return Power_Hex;
                case EffectStyle.Agility:
                    return Agility_Hex;
                case EffectStyle.Focus:
                    return Focus_Hex;
                case EffectStyle.Support:
                    return Support_Hex;
                case EffectStyle.Defense:
                    return Defense_Hex;
                default:
                    return "FFFFFF";
            }
        }

        public static GameColor GetColor(EffectStyle style)
        {
            uint color = Convert.ToUInt32(GetHex(style), 16);
            uint r = (color >> 16) & 255;
            uint g = (color >> 8) & 255;
            uint b = color & 255;
            return new GameColor((sbyte)(r - 128), (sbyte)(g - 128), (sbyte)(b - 128));
        }

        public static string ToRichText(EffectStyle style)
        {
            return $"<color=#{GetHex(style)}>{style}</color>";
        }

        public static void ParseXml(XmlParser xml, List<EffectStyle> dest)
        {
            if (dest == null) return;
            if (xml.Exists("Power")) dest.Add(EffectStyle.Power);
            if (xml.Exists("Agility")) dest.Add(EffectStyle.Agility);
            if (xml.Exists("Focus")) dest.Add(EffectStyle.Focus);
            if (xml.Exists("Defense")) dest.Add(EffectStyle.Defense);
            if (xml.Exists("Support")) dest.Add(EffectStyle.Support);
        }
    }
}
