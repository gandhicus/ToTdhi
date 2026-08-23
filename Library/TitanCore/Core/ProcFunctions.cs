using System;
using TitanCore.Data.Components;

namespace TitanCore.Core
{
    public static class ProcFunctions
    {
        public static int GetProcKey(ushort itemId, int procIndex)
        {
            return (itemId << 8) | procIndex;
        }

        public static string GetStatDisplayName(StatType type)
        {
            switch (type)
            {
                case StatType.MaxHealth:
                    return "Max Health";
                default:
                    return type.ToString();
            }
        }

        public static string GetAlternateStatDisplayName(AlternateStatType type)
        {
            switch (type)
            {
                case AlternateStatType.RateOfFire:
                    return "Rate of Fire";
                case AlternateStatType.TrueDamageChance:
                    return "True Damage Chance";
                case AlternateStatType.BlockChance:
                    return "Block Chance";
                case AlternateStatType.AbsorptionChance:
                    return "Absorption Chance";
                case AlternateStatType.CriticalStrikeChance:
                    return "Critical Strike Chance";
                case AlternateStatType.CriticalStrikeDamage:
                    return "Critical Strike Damage";
                case AlternateStatType.RageGain:
                    return "Rage Gain";
                case AlternateStatType.GroundedResistance:
                    return "Grounded Resistance";
                case AlternateStatType.KnockbackResistance:
                    return "Knockback Resistance";
                default:
                    return type.ToString();
            }
        }

        public static string FormatAlternateStatAmount(AlternateStatType type, int amount)
        {
            switch (type)
            {
                case AlternateStatType.RateOfFire:
                case AlternateStatType.TrueDamageChance:
                case AlternateStatType.BlockChance:
                case AlternateStatType.AbsorptionChance:
                case AlternateStatType.CriticalStrikeChance:
                case AlternateStatType.CriticalStrikeDamage:
                case AlternateStatType.RageGain:
                case AlternateStatType.GroundedResistance:
                case AlternateStatType.KnockbackResistance:
                    return (amount > 0 ? "+" : "") + amount + "%";
                default:
                    return (amount > 0 ? "+" : "") + amount;
            }
        }

        public static ProcTrigger? HitResultToTrigger(HitResultType type)
        {
            switch (type)
            {
                case HitResultType.Absorbed:
                    return ProcTrigger.Absorption;
                case HitResultType.Blocked:
                    return ProcTrigger.Block;
                case HitResultType.Critical:
                    return ProcTrigger.CriticalStrike;
                case HitResultType.TrueDamage:
                    return ProcTrigger.TrueDamage;
                default:
                    return null;
            }
        }

        public static StatusEffect? GetStatBonusEffect(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth:
                    return StatusEffect.MaxHealthBonus;
                case StatType.Speed:
                    return StatusEffect.SpeedBonus;
                case StatType.Attack:
                    return StatusEffect.AttackBonus;
                case StatType.Defense:
                    return StatusEffect.DefenseBonus;
                case StatType.Vigor:
                    return StatusEffect.VigorBonus;
                default:
                    return null;
            }
        }

        public static StatusEffect? GetAlternateStatBonusEffect(AlternateStatType stat)
        {
            switch (stat)
            {
                case AlternateStatType.TrueDamageChance:
                    return StatusEffect.TrueBonus;
                case AlternateStatType.BlockChance:
                    return StatusEffect.BlockBonus;
                case AlternateStatType.CriticalStrikeChance:
                    return StatusEffect.CritBonus;
                case AlternateStatType.AbsorptionChance:
                    return StatusEffect.AbsorptionBonus;
                case AlternateStatType.RateOfFire:
                    return StatusEffect.RateOfFireBonus;
                default:
                    return null;
            }
        }

        public static string GetTriggerDisplayName(ProcTrigger trigger)
        {
            switch (trigger)
            {
                case ProcTrigger.Absorption:
                    return "Absorption";
                case ProcTrigger.Block:
                    return "Block";
                case ProcTrigger.CriticalStrike:
                    return "Critical Strike";
                case ProcTrigger.TrueDamage:
                    return "True Damage";
                default:
                    return trigger.ToString();
            }
        }

        public static string FormatDurationSeconds(uint durationMs)
        {
            if (durationMs <= 0) return "0";
            var seconds = durationMs / 1000f;
            if (Math.Abs(seconds - Math.Round(seconds)) < 0.001f)
                return ((int)Math.Round(seconds)).ToString();
            return seconds.ToString("0.#");
        }

        public static string FormatCooldownSeconds(uint cooldownMs)
        {
            if (cooldownMs <= 0) return "0";
            var seconds = cooldownMs / 1000f;
            if (Math.Abs(seconds - Math.Round(seconds)) < 0.001f)
                return ((int)Math.Round(seconds)).ToString();
            return seconds.ToString("0.#");
        }

        public static string GetProcTooltipText(ItemProc proc)
        {
            var trigger = GetTriggerDisplayName(proc.trigger);
            var cooldownText = proc.cooldownMs > 0
                ? $" ({FormatCooldownSeconds(proc.cooldownMs)} second cooldown)"
                : "";

            if (proc.statBonus != null)
            {
                var durationText = FormatDurationSeconds(proc.statBonus.durationMs);
                return $"On {trigger}, gain +{proc.statBonus.amount} {GetStatDisplayName(proc.statBonus.statType)} for {durationText} seconds{cooldownText}.";
            }

            if (proc.alternateStatBonus != null)
            {
                var durationText = FormatDurationSeconds(proc.alternateStatBonus.durationMs);
                var amountText = FormatAlternateStatAmount(proc.alternateStatBonus.statType, proc.alternateStatBonus.amount);
                return $"On {trigger}, gain {amountText} {GetAlternateStatDisplayName(proc.alternateStatBonus.statType)} for {durationText} seconds{cooldownText}.";
            }

            if (proc.rageGain != null)
            {
                var rageAmount = proc.rageGain.amount;
                var rageText = Math.Abs(rageAmount - Math.Round(rageAmount)) < 0.001f
                    ? ((int)Math.Round(rageAmount)).ToString()
                    : rageAmount.ToString("0.#");
                return $"On {trigger}, gain +{rageText} Rage{cooldownText}.";
            }

            return "";
        }

        public static string GetScaledStatTooltipText(ScaledStatIncrease scaled, int currentBonus = -1)
        {
            var toStatName = scaled.toIsAlternate
                ? GetAlternateStatDisplayName(scaled.toAlternateStat)
                : GetStatDisplayName(scaled.toStat);
            var gainText = scaled.toIsAlternate
                ? FormatAlternateStatAmount(scaled.toAlternateStat, scaled.gainAmount)
                : scaled.gainAmount.ToString();
            var fromStatName = scaled.fromIsAlternate
                ? GetAlternateStatDisplayName(scaled.fromAlternateStat)
                : GetStatDisplayName(scaled.fromStat);
            var text = $"Every {scaled.perAmount} {fromStatName}, gain {gainText} {toStatName}";
            if (currentBonus >= 0)
            {
                var currentText = scaled.toIsAlternate
                    ? FormatAlternateStatAmount(scaled.toAlternateStat, currentBonus)
                    : $"+{currentBonus}";
                text += $" (currently {currentText})";
            }
            return text + ".";
        }
    }
}
