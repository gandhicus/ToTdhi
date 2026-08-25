using System;

namespace TitanCore.Core
{
    /// <summary>
    /// Skill tree definitions per class. Each nested *Ranks class holds per-rank constants
    /// used by both <see cref="ApplyRanks"/> (gameplay) and tooltip strings (UI).
    /// After edits, rebuild TitanCore (SyncTitanCoreToUnity.ps1) and restart the server.
    /// </summary>
    public static class ClassSkillTrees
    {
        public struct NodeDef
        {
            public string name;
            public string sprite;
            public EffectStyle style;
            public Func<int, string> effect;
        }

        private static class WarriorRanks
        {
            public const float CleaveWeaponPct = 0.04f;
            public const int HasteCooldownMs = 1000;
            public const int WillLockoutMs = 50;
            public const float FrustrationKeep = 0.04f;
            public const int EverlastingMs = 500;
            public const float MendingHeal = 0.03f;
            public const int AegisDefense = 4;
            public const int CastleMaxHealth = 10;
        }

        private static class RangerRanks
        {
            public const float WrathDamagePct = 0.15f;
            public const float ManifestRadius = 0.8f;
            public const int EverlastingMs = 80;
            public const float FrustrationKeep = 0.08f;
            public const float HasteCooldownPct = 0.05f;
            public const float UnfurlRange = 1f;
            public const int EnigmaSlowMs = 400;
            public const int GriefRageOnKill = 4;
        }

        private static class LancerRanks
        {
            public const float WrathDamagePct = 0.08f;
            public const float FrustrationRageCost = 0.5f;
            public const float ManifestSizePct = 0.10f;
            public const int GriefRageOnKill = 3;
            public const int HasteCooldownMs = 8;
            public const float AttunedWobblePct = 0.10f;
            public const int BlightAttack = 2;
            public const int BlightMs = 600;
        }

        private static class BladeweaverRanks
        {
            public const float WrathDamagePct = 0.03f;
            public const float UnfurlRange = 0.4f;
            public const int EverlastingMs = 20;
            public const float FrustrationKeep = 0.06f;
            public const int HasteCooldownMs = 1000;
            public const float ManifestSizePct = 0.15f;
            public const int AlacritySpeed = 2;
            public const int AlacrityMs = 300;
            public const int AegisInvulnMs = 150;
        }

        private static class NomadRanks
        {
            public const float WrathMarkedDamagePct = 0.08f;
            public const int FrustrationMarkedRage = 1;
            public const int MendingInteractHeal = 10;
            public const int EverlastingMs = 2000;
            public const float HasteCooldownPct = 0.08f;
            public const float ManifestRadius = 0.3f;
            public const int FlickerRofMs = 500;
            public const int ResonateLingerMs = 1000;
        }

        private static class BrewerRanks
        {
            public const int EverlastingMs = 500;
            public const float ManifestRadius = 0.6f;
            public const int FlickerRof = 2;
            public const int MendingVigor = 3;
            public const float HasteCooldownPct = 0.05f;
            public const float FrustrationKeep = 0.08f;
            public const int EnigmaSlowMs = 800;
            public const int AegisDefense = 4;
            public const int AegisDefenseMs = 2000;
        }

        private static class CommanderRanks
        {
            public const float EverlastingDurationPct = 0.08f;
            public const float UnfurlRange = 0.3f;
            public const int BrandishMs = 500;
            public const int WillLockoutMs = 50;
            public const int HasteCooldownMs = 1000;
            public const float FrustrationKeep = 0.08f;
            public const int AegisDefense = 3;
            public const int CastleMaxHealth = 10;
        }

        private static class MinisterRanks
        {
            public const float MendingHeal = 0.08f;
            public const float ManifestRadius = 0.5f;
            public const int WillLockoutMs = 100;
            public const int EverlastingMs = 1000;
            public const float HasteCooldownPct = 0.05f;
            public const int PurifyVigor = 3;
            public const int WrathAttack = 3;
            public const int AegisDefense = 3;
        }

        private static class AlchemistRanks
        {
            public const float WrathDamagePct = 0.20f;
            public const float ManifestRadius = 0.5f;
            public const int WillLockoutMs = 50;
            public const float EverlastingDurationPct = 0.10f;
            public const float HasteCooldownPct = 0.05f;
            public const float FrustrationKeep = 0.08f;
            public const int BlightAttack = 1;
            public const int EnigmaSlowMs = 300;
        }

        private static class BerserkerRanks
        {
            public const float WrathDamagePct = 0.20f;
            public const float UnfurlRange = 0.6f;
            public const float ManifestSpreadDeg = 6f;
            public const int EnigmaSlowMs = 600;
            public const float HasteCooldownPct = 0.05f;
            public const int EverlastingRofMs = 500;
            public const int FlickerRof = 2;
            public const int BlightAttack = 3;
            public const int BlightMs = 2000;
        }

        public static NodeDef[] GetNodes(ClassType classType)
        {
            switch (classType)
            {
                case ClassType.Warrior: return Warrior;
                case ClassType.Ranger: return Ranger;
                case ClassType.Lancer: return Lancer;
                case ClassType.Bladeweaver: return Bladeweaver;
                case ClassType.Nomad: return Nomad;
                case ClassType.Brewer: return Brewer;
                case ClassType.Commander: return Commander;
                case ClassType.Minister: return Minister;
                case ClassType.Alchemist: return Alchemist;
                case ClassType.Berserker: return Berserker;
                default: return Warrior;
            }
        }

        public static void ApplyRanks(ClassType classType, int[] r, ref AbilityModifierSnapshot snap)
        {
            switch (classType)
            {
                case ClassType.Warrior:
                    snap.weaponDamagePct = SkillTreeFunctions.Scale(WarriorRanks.CleaveWeaponPct, r[0]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(WarriorRanks.HasteCooldownMs, r[1]);
                    snap.pulseLockoutMs = SkillTreeFunctions.Base_Pulse_Lockout_Ms - SkillTreeFunctions.Scale(WarriorRanks.WillLockoutMs, r[2]);
                    snap.rageKeep = SkillTreeFunctions.Scale(WarriorRanks.FrustrationKeep, r[3]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(WarriorRanks.EverlastingMs, r[4]);
                    snap.healPower = SkillTreeFunctions.Scale(WarriorRanks.MendingHeal, r[5]);
                    snap.hymnDefense = SkillTreeFunctions.Scale(WarriorRanks.AegisDefense, r[6]);
                    snap.hymnMaxHealth = SkillTreeFunctions.Scale(WarriorRanks.CastleMaxHealth, r[7]);
                    break;
                case ClassType.Ranger:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(RangerRanks.WrathDamagePct, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(RangerRanks.ManifestRadius, r[1]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(RangerRanks.EverlastingMs, r[2]);
                    snap.rageKeep = SkillTreeFunctions.Scale(RangerRanks.FrustrationKeep, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(RangerRanks.HasteCooldownPct, r[4]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(RangerRanks.UnfurlRange, r[5]);
                    snap.slowMs = SkillTreeFunctions.Scale(RangerRanks.EnigmaSlowMs, r[6]);
                    snap.rageOnKill = SkillTreeFunctions.Scale(RangerRanks.GriefRageOnKill, r[7]);
                    break;
                case ClassType.Lancer:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(LancerRanks.WrathDamagePct, r[0]);
                    snap.rageCostFlat = SkillTreeFunctions.Scale(LancerRanks.FrustrationRageCost, r[1]);
                    snap.projectileSizePct = SkillTreeFunctions.Scale(LancerRanks.ManifestSizePct, r[2]);
                    snap.rageOnKill = SkillTreeFunctions.Scale(LancerRanks.GriefRageOnKill, r[3]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(LancerRanks.HasteCooldownMs, r[4]);
                    snap.wobbleMul = 1f - SkillTreeFunctions.Scale(LancerRanks.AttunedWobblePct, r[5]);
                    snap.pierce = r[6] / 2;
                    snap.timedAttack = SkillTreeFunctions.Scale(LancerRanks.BlightAttack, r[7]);
                    snap.timedAttackMs = r[7] > 0 ? LancerRanks.BlightMs : 0;
                    break;
                case ClassType.Bladeweaver:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(BladeweaverRanks.WrathDamagePct, r[0]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(BladeweaverRanks.UnfurlRange, r[1]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(BladeweaverRanks.EverlastingMs, r[2]);
                    snap.rageKeep = SkillTreeFunctions.Scale(BladeweaverRanks.FrustrationKeep, r[3]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(BladeweaverRanks.HasteCooldownMs, r[4]);
                    snap.projectileSizePct = SkillTreeFunctions.Scale(BladeweaverRanks.ManifestSizePct, r[5]);
                    snap.speedOnHitMs = r[6] > 0 ? SkillTreeFunctions.Scale(BladeweaverRanks.AlacrityMs, r[6]) : 0;
                    snap.speedOnHit = r[6] > 0 ? BladeweaverRanks.AlacritySpeed : 0;
                    snap.postDashInvulnMs = SkillTreeFunctions.Scale(BladeweaverRanks.AegisInvulnMs, r[7]);
                    break;
                case ClassType.Nomad:
                    snap.markedDamagePct = SkillTreeFunctions.Scale(NomadRanks.WrathMarkedDamagePct, r[0]);
                    snap.markedRage = SkillTreeFunctions.Scale(NomadRanks.FrustrationMarkedRage, r[1]);
                    snap.interactHealBonus = SkillTreeFunctions.Scale(NomadRanks.MendingInteractHeal, r[2]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(NomadRanks.EverlastingMs, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(NomadRanks.HasteCooldownPct, r[4]);
                    snap.markRadiusBonus = SkillTreeFunctions.Scale(NomadRanks.ManifestRadius, r[5]);
                    snap.rofDurationBonusMs = SkillTreeFunctions.Scale(NomadRanks.FlickerRofMs, r[6]);
                    snap.markedLingerMs = SkillTreeFunctions.Scale(NomadRanks.ResonateLingerMs, r[7]);
                    break;
                case ClassType.Brewer:
                    snap.durationBonusMs = SkillTreeFunctions.Scale(BrewerRanks.EverlastingMs, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(BrewerRanks.ManifestRadius, r[1]);
                    snap.rofAmount = SkillTreeFunctions.Scale(BrewerRanks.FlickerRof, r[2]);
                    snap.vigorBonus = SkillTreeFunctions.Scale(BrewerRanks.MendingVigor, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(BrewerRanks.HasteCooldownPct, r[4]);
                    snap.rageKeep = SkillTreeFunctions.Scale(BrewerRanks.FrustrationKeep, r[5]);
                    snap.slowMs = SkillTreeFunctions.Scale(BrewerRanks.EnigmaSlowMs, r[6]);
                    snap.hymnDefense = SkillTreeFunctions.Scale(BrewerRanks.AegisDefense, r[7]);
                    snap.timedDefenseMs = r[7] > 0 ? BrewerRanks.AegisDefenseMs : 0;
                    break;
                case ClassType.Commander:
                    snap.durationMul = 1f + SkillTreeFunctions.Scale(CommanderRanks.EverlastingDurationPct, r[0]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(CommanderRanks.UnfurlRange, r[1]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(CommanderRanks.BrandishMs, r[2]);
                    snap.pulseLockoutMs = 500 - SkillTreeFunctions.Scale(CommanderRanks.WillLockoutMs, r[3]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(CommanderRanks.HasteCooldownMs, r[4]);
                    snap.rageKeep = SkillTreeFunctions.Scale(CommanderRanks.FrustrationKeep, r[5]);
                    snap.hymnDefense = SkillTreeFunctions.Scale(CommanderRanks.AegisDefense, r[6]);
                    snap.hymnMaxHealth = SkillTreeFunctions.Scale(CommanderRanks.CastleMaxHealth, r[7]);
                    break;
                case ClassType.Minister:
                    snap.healPower = SkillTreeFunctions.Scale(MinisterRanks.MendingHeal, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(MinisterRanks.ManifestRadius, r[1]);
                    snap.pulseLockoutMs = 2000 - SkillTreeFunctions.Scale(MinisterRanks.WillLockoutMs, r[2]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(MinisterRanks.EverlastingMs, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(MinisterRanks.HasteCooldownPct, r[4]);
                    snap.vigorBonus = SkillTreeFunctions.Scale(MinisterRanks.PurifyVigor, r[5]);
                    snap.timedAttack = SkillTreeFunctions.Scale(MinisterRanks.WrathAttack, r[6]);
                    snap.fieldDefense = SkillTreeFunctions.Scale(MinisterRanks.AegisDefense, r[7]);
                    break;
                case ClassType.Alchemist:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(AlchemistRanks.WrathDamagePct, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(AlchemistRanks.ManifestRadius, r[1]);
                    snap.pulseLockoutMs = 1000 - SkillTreeFunctions.Scale(AlchemistRanks.WillLockoutMs, r[2]);
                    snap.durationMul = 1f + SkillTreeFunctions.Scale(AlchemistRanks.EverlastingDurationPct, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(AlchemistRanks.HasteCooldownPct, r[4]);
                    snap.rageKeep = SkillTreeFunctions.Scale(AlchemistRanks.FrustrationKeep, r[5]);
                    snap.timedAttack = SkillTreeFunctions.Scale(AlchemistRanks.BlightAttack, r[6]);
                    snap.slowMs = SkillTreeFunctions.Scale(AlchemistRanks.EnigmaSlowMs, r[7]);
                    break;
                case ClassType.Berserker:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(BerserkerRanks.WrathDamagePct, r[0]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(BerserkerRanks.UnfurlRange, r[1]);
                    snap.shoutSpreadDeg = SkillTreeFunctions.Scale(BerserkerRanks.ManifestSpreadDeg, r[2]);
                    snap.slowMs = SkillTreeFunctions.Scale(BerserkerRanks.EnigmaSlowMs, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(BerserkerRanks.HasteCooldownPct, r[4]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(BerserkerRanks.EverlastingRofMs, r[5]);
                    snap.rofAmount = SkillTreeFunctions.Scale(BerserkerRanks.FlickerRof, r[6]);
                    snap.timedAttack = SkillTreeFunctions.Scale(BerserkerRanks.BlightAttack, r[7]);
                    snap.timedAttackMs = r[7] > 0 ? BerserkerRanks.BlightMs : 0;
                    break;
            }
        }

        public static readonly NodeDef[] Warrior =
        {
            N("Cleave", "Warrior/Cleave", EffectStyle.Power, r => $"+{Pct(WarriorRanks.CleaveWeaponPct, r)} weapon damage on ability pulse"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(WarriorRanks.HasteCooldownMs, r)} ability cooldown"),
            N("Will", "Warrior/Will", EffectStyle.Power, r => $"Ability pulses {SkillTreeFunctions.Scale(WarriorRanks.WillLockoutMs, r)} ms more often"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(WarriorRanks.FrustrationKeep, r)} rage after ability use"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(WarriorRanks.EverlastingMs, r)} ability duration"),
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Pct(WarriorRanks.MendingHeal, r)} ability heal"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(WarriorRanks.AegisDefense, r)} Defense while ability is active"),
            N("Castle", "Warrior/Castle", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(WarriorRanks.CastleMaxHealth, r)} Max Health while ability is active"),
        };

        public static readonly NodeDef[] Ranger =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(RangerRanks.WrathDamagePct, r)} ability damage"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(RangerRanks.ManifestRadius, r):0.#} tile ability radius"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(RangerRanks.EverlastingMs, r)} ms ability duration"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(RangerRanks.FrustrationKeep, r)} rage after ability use"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(RangerRanks.HasteCooldownPct, r)} ability cooldown"),
            N("Unfurl", "Ranger/Unfurl", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(RangerRanks.UnfurlRange, r)} tile ability range"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(RangerRanks.EnigmaSlowMs, r)}"),
            N("Grief", "Ranger/Grief", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(RangerRanks.GriefRageOnKill, r)} rage on ability kill"),
        };

        public static readonly NodeDef[] Lancer =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(LancerRanks.WrathDamagePct, r)} ability damage"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"-{SkillTreeFunctions.Scale(LancerRanks.FrustrationRageCost, r):0.#} rage cost"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Pct(LancerRanks.ManifestSizePct, r)} ability size"),
            N("Grief", "Ranger/Grief", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(LancerRanks.GriefRageOnKill, r)} rage on ability kill"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SkillTreeFunctions.Scale(LancerRanks.HasteCooldownMs, r)} ms ability cooldown"),
            N("Attuned", "Lancer/Attuned", EffectStyle.Power, r => $"-{Pct(LancerRanks.AttunedWobblePct, r)} angle wobble"),
            N("Piercing", "Lancer/Piercing", EffectStyle.Power, r => $"+{r / 2} pierce"),
            N("Blight", "Lancer/Blight", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(LancerRanks.BlightAttack, r)} Attack for {LancerRanks.BlightMs / 1000f:0.#}s after ability use"),
        };

        public static readonly NodeDef[] Bladeweaver =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(BladeweaverRanks.WrathDamagePct, r)} ability damage"),
            N("Unfurl", "Bladeweaver/Unfurl", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(BladeweaverRanks.UnfurlRange, r):0.#} tile ability range"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(BladeweaverRanks.EverlastingMs, r)} ms ability duration"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(BladeweaverRanks.FrustrationKeep, r)} rage after ability hit"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(BladeweaverRanks.HasteCooldownMs, r)} ability cooldown"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Pct(BladeweaverRanks.ManifestSizePct, r)} ability size"),
            N("Alacrity", "Bladeweaver/Alacrity", EffectStyle.Agility, r => $"+{BladeweaverRanks.AlacritySpeed} Speed for {Sec(BladeweaverRanks.AlacrityMs, r)} on ability hit"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{Sec(BladeweaverRanks.AegisInvulnMs, r)} Invulnerable after ability ends"),
        };

        public static readonly NodeDef[] Nomad =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(NomadRanks.WrathMarkedDamagePct, r)} damage to Marked enemies"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(NomadRanks.FrustrationMarkedRage, r)} rage when hitting Marked enemies"),
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(NomadRanks.MendingInteractHeal, r)} heal on charm interact"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SecI(NomadRanks.EverlastingMs, r)} ability duration"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(NomadRanks.HasteCooldownPct, r)} ability cooldown"),
            N("Manifest", "Nomad/ManifestFocus", EffectStyle.Focus, r => $"+{SkillTreeFunctions.Scale(NomadRanks.ManifestRadius, r):0.##} tile ability radius"),
            N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{Sec(NomadRanks.FlickerRofMs, r)} Rate of Fire duration"),
            N("Resonate", "Nomad/Resonate", EffectStyle.Focus, r => $"+{Sec(NomadRanks.ResonateLingerMs, r)} Marked duration"),
        };

        public static readonly NodeDef[] Brewer =
        {
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(BrewerRanks.EverlastingMs, r)} ability duration"),
            N("Manifest", "Brewer/ManifestSupport", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(BrewerRanks.ManifestRadius, r):0.#} tile ability radius"),
            N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(BrewerRanks.FlickerRof, r)}% RoF after drinking purple brew"),
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(BrewerRanks.MendingVigor, r)} Vigor after drinking red brew"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(BrewerRanks.HasteCooldownPct, r)} ability cooldown"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(BrewerRanks.FrustrationKeep, r)} rage after ability use"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(BrewerRanks.EnigmaSlowMs, r)}"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(BrewerRanks.AegisDefense, r)} Defense for {BrewerRanks.AegisDefenseMs / 1000f:0.#}s after ability use"),
        };

        public static readonly NodeDef[] Commander =
        {
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Pct(CommanderRanks.EverlastingDurationPct, r)} ability duration"),
            N("Unfurl", "Bladeweaver/Unfurl", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(CommanderRanks.UnfurlRange, r):0.#} tile ability range"),
            N("Brandish", "Commander/Brandish", EffectStyle.Agility, r => $"+{Sec(CommanderRanks.BrandishMs, r)} range duration"),
            N("Will", "Warrior/Will", EffectStyle.Power, r => $"Ability pulses {SkillTreeFunctions.Scale(CommanderRanks.WillLockoutMs, r)} ms more often"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(CommanderRanks.HasteCooldownMs, r)} ability cooldown"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(CommanderRanks.FrustrationKeep, r)} rage after ability use"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(CommanderRanks.AegisDefense, r)} Defense while ability is active"),
            N("Castle", "Warrior/Castle", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(CommanderRanks.CastleMaxHealth, r)} Max Health while ability is active"),
        };

        public static readonly NodeDef[] Minister =
        {
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Pct(MinisterRanks.MendingHeal, r)} ability heal"),
            N("Manifest", "Brewer/ManifestSupport", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(MinisterRanks.ManifestRadius, r):0.#} tile ability radius"),
            N("Will", "Warrior/Will", EffectStyle.Agility, r => $"Ability heals {SkillTreeFunctions.Scale(MinisterRanks.WillLockoutMs, r)} ms more often"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(MinisterRanks.EverlastingMs, r)} ability duration"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(MinisterRanks.HasteCooldownPct, r)} ability cooldown"),
            N("Purify", "Brewer/Purify", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(MinisterRanks.PurifyVigor, r)} Vigor while ability is active"),
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(MinisterRanks.WrathAttack, r)} Attack while ability is active"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(MinisterRanks.AegisDefense, r)} Defense while ability is active"),
        };

        public static readonly NodeDef[] Alchemist =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(AlchemistRanks.WrathDamagePct, r)} ability damage"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(AlchemistRanks.ManifestRadius, r):0.#} tile ability radius"),
            N("Will", "Warrior/Will", EffectStyle.Agility, r => $"Ability ticks {SkillTreeFunctions.Scale(AlchemistRanks.WillLockoutMs, r)} ms more often"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Pct(AlchemistRanks.EverlastingDurationPct, r)} ability duration"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(AlchemistRanks.HasteCooldownPct, r)} ability cooldown"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(AlchemistRanks.FrustrationKeep, r)} rage after ability use"),
            N("Blight", "Lancer/Blight", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(AlchemistRanks.BlightAttack, r)} Attack while ability is active"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(AlchemistRanks.EnigmaSlowMs, r)}"),
        };

        public static readonly NodeDef[] Berserker =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(BerserkerRanks.WrathDamagePct, r)} ability damage"),
            N("Unfurl", "Ranger/Unfurl", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(BerserkerRanks.UnfurlRange, r):0.#} tile ability range"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(BerserkerRanks.ManifestSpreadDeg, r)}\u00b0 ability spread"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"+{Sec(BerserkerRanks.EnigmaSlowMs, r)} Slowed"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(BerserkerRanks.HasteCooldownPct, r)} ability cooldown"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(BerserkerRanks.EverlastingRofMs, r)} Rate of Fire duration"),
            N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(BerserkerRanks.FlickerRof, r)}% Rate of Fire"),
            N("Blight", "Lancer/Blight", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(BerserkerRanks.BlightAttack, r)} Attack for {BerserkerRanks.BlightMs / 1000f:0.#}s after ability use"),
        };

        private static NodeDef N(string name, string sprite, EffectStyle style, Func<int, string> effect)
        {
            return new NodeDef { name = name, sprite = sprite, style = style, effect = effect };
        }

        private static string Pct(float perRank, int r) => $"{SkillTreeFunctions.Scale(perRank, r) * 100:0}%";

        private static string Sec(int msPerRank, int r)
        {
            float s = SkillTreeFunctions.Scale(msPerRank, r) / 1000f;
            return $"{s:0.##}s";
        }

        private static string SecI(int msPerRank, int r)
        {
            float s = SkillTreeFunctions.Scale(msPerRank, r) / 1000f;
            if (Math.Abs(s - Math.Round(s)) < 0.001f)
                return $"{(int)Math.Round(s)}s";
            return $"{s:0.##}s";
        }
    }
}
