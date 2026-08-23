using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TitanCore.Core;
using Utils.NET.IO.Xml;

namespace TitanCore.Data.Components
{
    public class TalismanAoe
    {
        public float radius = 3f;

        public int damage;

        public bool trueDamage;

        public float range;

        public float lifetime = 0.35f;

        public TalismanAoeAt at = TalismanAoeAt.Target;

        public bool hasColor;

        public GameColor color = GameColor.yellow;

        public StatusEffectData[] statusEffects = Array.Empty<StatusEffectData>();

        public TalismanAoe(XmlParser xml)
        {
            radius = xml.AtrFloat("radius", xml.Float("Radius", 3f));
            damage = xml.AtrInt("damage", xml.Int("Damage", 0));
            var trueDamageText = xml.AtrString("trueDamage", xml.String("TrueDamage"));
            trueDamage = string.Equals(trueDamageText, "true", StringComparison.OrdinalIgnoreCase);
            range = xml.AtrFloat("range", xml.Float("Range", 0f));
            lifetime = xml.AtrFloat("lifetime", xml.Float("Lifetime", 0.35f));
            at = xml.AtrEnum("at", xml.Enum("At", TalismanAoeAt.Target));
            var colorText = xml.AtrString("color", xml.String("Color"));
            hasColor = !string.IsNullOrWhiteSpace(colorText);
            if (hasColor)
                color = GameColor.Parse(colorText);
            var effects = new List<StatusEffectData>();
            foreach (var child in xml.Elements("StatusEffect"))
                effects.Add(new StatusEffectData(child));
            statusEffects = effects.ToArray();
        }
    }

    public class TalismanEffect
    {
        public const int DefaultRageThreshold = 25;

        public TalismanTrigger trigger = TalismanTrigger.AbilityPulse;

        public int ragePercentThreshold;

        public uint cooldownMs;

        public ProcStatBonus statBonus;

        public ProcAlternateStatBonus alternateStatBonus;

        public float healMul = 1f;

        public int healAmount;

        public float rageGain;

        public int rofAmount;

        public float damageMul = 1f;

        public StatusEffectData[] statusEffects = Array.Empty<StatusEffectData>();

        public bool hasAoeColor;

        public GameColor aoeColor;

        public TalismanAoe aoe;

        public TalismanEffect(XmlParser xml)
        {
            trigger = xml.AtrEnum("trigger", TalismanTrigger.AbilityPulse);
            ragePercentThreshold = xml.AtrInt("rageThreshold", DefaultRageThreshold);
            cooldownMs = (uint)xml.AtrInt("cooldown", 0);

            foreach (var child in xml.Elements("StatBonus"))
                statBonus = new ProcStatBonus(child);

            foreach (var child in xml.Elements("AlternateStatBonus"))
                alternateStatBonus = new ProcAlternateStatBonus(child);

            if (xml.TryGetValue("HealMul", out var healMulElement))
                healMul = Convert.ToSingle(healMulElement.Value, CultureInfo.InvariantCulture);

            if (xml.TryGetValue("Heal", out var healElement))
                healAmount = Convert.ToInt32(healElement.Value, CultureInfo.InvariantCulture);

            if (xml.TryGetValue("Rage", out var rageElement))
                rageGain = Convert.ToSingle(rageElement.Value, CultureInfo.InvariantCulture);

            if (xml.TryGetValue("RateOfFire", out var rofElement))
                rofAmount = Convert.ToInt32(rofElement.Value, CultureInfo.InvariantCulture);

            if (xml.TryGetValue("DamageMul", out var dmgElement))
                damageMul = Convert.ToSingle(dmgElement.Value, CultureInfo.InvariantCulture);

            var targetEffects = new List<StatusEffectData>();
            foreach (var child in xml.Elements("StatusEffect"))
                targetEffects.Add(new StatusEffectData(child));
            statusEffects = targetEffects.ToArray();

            foreach (var child in xml.Elements("Aoe"))
                aoe = new TalismanAoe(child);

            if (xml.TryGetValue("AoeColor", out var aoeColorElement) && !string.IsNullOrWhiteSpace(aoeColorElement.Value))
            {
                hasAoeColor = true;
                aoeColor = GameColor.Parse(aoeColorElement.Value);
            }
            else if (xml.AtrExists("aoeColor"))
            {
                var atr = xml.AtrString("aoeColor");
                if (!string.IsNullOrWhiteSpace(atr))
                {
                    hasAoeColor = true;
                    aoeColor = GameColor.Parse(atr);
                }
            }
        }

        public static bool TryGetAbilityAoeColor(IList<TalismanEffect> effects, out GameColor color)
        {
            color = GameColor.white;
            if (effects == null) return false;
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null || !effect.hasAoeColor) continue;
                color = effect.aoeColor;
                return true;
            }
            return false;
        }

        public static void ApplyAbilityAoeColor(WorldEffect worldEffect, IList<TalismanEffect> effects)
        {
            if (worldEffect == null) return;
            if (!TryGetAbilityAoeColor(effects, out var color)) return;
            worldEffect.hasColor = true;
            worldEffect.color = color;
        }

        public static TalismanEffect CreateDefensePulse()
        {
            return new TalismanEffect(TalismanTrigger.AbilityPulse, 0, new ProcStatBonus(StatType.Defense, 8, 1200), 0.90f, null);
        }

        private TalismanEffect(TalismanTrigger trigger, uint cooldownMs, ProcStatBonus statBonus, float healMul, TalismanAoe aoe)
        {
            this.trigger = trigger;
            this.cooldownMs = cooldownMs;
            this.statBonus = statBonus;
            this.healMul = healMul;
            this.aoe = aoe;
        }

        public string Describe()
        {
            var parts = new List<string>();
            if (statBonus != null && statBonus.amount != 0)
            {
                parts.Add($"gain {Highlight($"+{statBonus.amount} {ProcFunctions.GetStatDisplayName(statBonus.statType)}")} for {Highlight($"{ProcFunctions.FormatDurationSeconds(statBonus.durationMs)} seconds")}");
            }

            if (alternateStatBonus != null && alternateStatBonus.amount != 0)
            {
                parts.Add($"gain {Highlight($"{ProcFunctions.FormatAlternateStatAmount(alternateStatBonus.statType, alternateStatBonus.amount)} {ProcFunctions.GetAlternateStatDisplayName(alternateStatBonus.statType)}")} for {Highlight($"{ProcFunctions.FormatDurationSeconds(alternateStatBonus.durationMs)} seconds")}");
            }

            if (healAmount > 0)
                parts.Add($"heal {Highlight($"{healAmount} HP")}");
            else if (healAmount < 0)
                parts.Add($"heal {Highlight($"{healAmount} HP")}");

            if (Math.Abs(rageGain) > 0.001f)
                parts.Add($"gain {Highlight($"+{rageGain:0.#} rage")}");

            if (rofAmount != 0)
                parts.Add($"gain {Highlight($"+{rofAmount}% Rate of Fire")}");

            if (Math.Abs(damageMul - 1f) > 0.001f)
                parts.Add($"deal {Highlight($"x{damageMul:0.##}")} damage");

            if (statusEffects != null && statusEffects.Length > 0)
            {
                var effects = new StringBuilder();
                for (int i = 0; i < statusEffects.Length; i++)
                {
                    if (i > 0)
                        effects.Append(i == statusEffects.Length - 1 ? " and " : ", ");
                    var hit = statusEffects[i];
                    effects.Append(Highlight(DescribeStatusEffect(hit)));
                }
                parts.Add($"apply {effects}");
            }

            if (aoe != null)
            {
                string aoeText;
                if (aoe.at == TalismanAoeAt.Self)
                    aoeText = $"release a {Highlight($"{aoe.radius:0.#} tile")} AoE around you";
                else if (aoe.at == TalismanAoeAt.RandomTarget)
                    aoeText = $"fire a {Highlight($"{aoe.radius:0.#} tile")} AoE at a random enemy";
                else
                    aoeText = $"shoot a {Highlight($"{aoe.radius:0.#} tile")} AoE";
                if (aoe.range > 0 && aoe.at == TalismanAoeAt.RandomTarget)
                    aoeText += $" within {Highlight($"{aoe.range:0.#} tiles")}";
                else if (aoe.range > 0 && aoe.at != TalismanAoeAt.Self)
                    aoeText += $" at {Highlight($"{aoe.range:0.#} range")}";
                if (aoe.damage > 0)
                {
                    aoeText += aoe.trueDamage
                        ? $" dealing {Highlight($"{aoe.damage} true damage")}"
                        : $" dealing {Highlight($"{aoe.damage} damage")}";
                }
                if (aoe.statusEffects != null && aoe.statusEffects.Length > 0)
                {
                    var effects = new StringBuilder();
                    for (int i = 0; i < aoe.statusEffects.Length; i++)
                    {
                        if (i > 0)
                            effects.Append(i == aoe.statusEffects.Length - 1 ? " and " : ", ");
                        var hit = aoe.statusEffects[i];
                        effects.Append(Highlight(DescribeStatusEffect(hit)));
                    }
                    aoeText += $" that applies {effects}";
                }
                parts.Add(aoeText);
            }

            var cooldownText = cooldownMs > 0
                ? $" ({Highlight($"{ProcFunctions.FormatCooldownSeconds(cooldownMs)} second cooldown")})"
                : "";
            var body = parts.Count > 0 ? string.Join(", ", parts) : "trigger";
            var triggerName = trigger == TalismanTrigger.AbilityUse && ragePercentThreshold > 0
                ? $"ability use at {Highlight($"{ragePercentThreshold}%")} rage or higher"
                : GetTriggerDisplayName(trigger);
            var text = $"On {triggerName}, {body}{cooldownText}.";
            if (ragePercentThreshold > 0 && trigger != TalismanTrigger.AbilityUse)
                text += $" Requires ability use at {Highlight($"{ragePercentThreshold}%")} rage or higher.";

            if (healMul > 0f && Math.Abs(healMul - 1f) > 0.001f)
                text += $" Ability heal {Highlight($"x{healMul:0.##}")}.";
            return text;
        }

        private static string DescribeStatusEffect(StatusEffectData hit)
        {
            string name;
            if (hit.type == StatusEffect.DefenseMinus)
                name = hit.amount != 0 ? $"-{Math.Abs(hit.amount)} Defense" : "Defense Minus";
            else
                name = hit.type.ToString();

            if (hit.duration > 0)
                return $"{name} for {ProcFunctions.FormatDurationSeconds(hit.duration)} seconds";
            return name;
        }

        private static string Highlight(string value)
        {
            return $"<color=#FFFFFF>{value}</color>";
        }

        public static string GetTriggerDisplayName(TalismanTrigger trigger)
        {
            switch (trigger)
            {
                case TalismanTrigger.AbilityUse:
                    return "ability use";
                case TalismanTrigger.AbilityPulse:
                    return "each ability pulse";
                case TalismanTrigger.AbilityHit:
                    return "ability hit";
                case TalismanTrigger.AbilityTick:
                    return "each ability tick";
                case TalismanTrigger.AbilityEnd:
                    return "ability end";
                case TalismanTrigger.HitMarked:
                    return "hitting a Marked enemy";
                case TalismanTrigger.Interact:
                    return "interact";
                default:
                    return trigger.ToString();
            }
        }
    }
}
