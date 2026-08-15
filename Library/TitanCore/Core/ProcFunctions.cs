using System;
using TitanCore.Data.Components;

namespace TitanCore.Core
{
    public static class ProcFunctions
    {
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
            var text = $"Every {scaled.perAmount} {GetStatDisplayName(scaled.fromStat)}, gain {scaled.gainAmount} {GetStatDisplayName(scaled.toStat)}";
            if (currentBonus >= 0)
                text += $" (currently +{currentBonus})";
            return text + ".";
        }
    }
}
