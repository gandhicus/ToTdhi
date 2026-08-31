using System;

namespace TitanCore.Core
{
    /// <summary>
    /// Skill tree definitions per class. Each nested *Tree holds rank constants, tooltip
    /// copy, and snapshot apply together so a class can be read in one place.
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

        public static NodeDef[] GetNodes(ClassType classType)
        {
            switch (classType)
            {
                case ClassType.Ranger: return RangerTree.Nodes;
                case ClassType.Lancer: return LancerTree.Nodes;
                case ClassType.Bladeweaver: return BladeweaverTree.Nodes;
                case ClassType.Nomad: return NomadTree.Nodes;
                case ClassType.Brewer: return BrewerTree.Nodes;
                case ClassType.Commander: return CommanderTree.Nodes;
                case ClassType.Minister: return MinisterTree.Nodes;
                case ClassType.Alchemist: return AlchemistTree.Nodes;
                case ClassType.Berserker: return BerserkerTree.Nodes;
                default: return WarriorTree.Nodes;
            }
        }

        public static void ApplyRanks(ClassType classType, int[] r, ref AbilityModifierSnapshot snap)
        {
            // Unknown classes (e.g. Sharpshooter) leave the snapshot unmodified.
            switch (classType)
            {
                case ClassType.Warrior: WarriorTree.Apply(r, ref snap); break;
                case ClassType.Ranger: RangerTree.Apply(r, ref snap); break;
                case ClassType.Lancer: LancerTree.Apply(r, ref snap); break;
                case ClassType.Bladeweaver: BladeweaverTree.Apply(r, ref snap); break;
                case ClassType.Nomad: NomadTree.Apply(r, ref snap); break;
                case ClassType.Brewer: BrewerTree.Apply(r, ref snap); break;
                case ClassType.Commander: CommanderTree.Apply(r, ref snap); break;
                case ClassType.Minister: MinisterTree.Apply(r, ref snap); break;
                case ClassType.Alchemist: AlchemistTree.Apply(r, ref snap); break;
                case ClassType.Berserker: BerserkerTree.Apply(r, ref snap); break;
            }
        }

        public static NodeDef[] Warrior => WarriorTree.Nodes;
        public static NodeDef[] Ranger => RangerTree.Nodes;
        public static NodeDef[] Lancer => LancerTree.Nodes;
        public static NodeDef[] Bladeweaver => BladeweaverTree.Nodes;
        public static NodeDef[] Nomad => NomadTree.Nodes;
        public static NodeDef[] Brewer => BrewerTree.Nodes;
        public static NodeDef[] Commander => CommanderTree.Nodes;
        public static NodeDef[] Minister => MinisterTree.Nodes;
        public static NodeDef[] Alchemist => AlchemistTree.Nodes;
        public static NodeDef[] Berserker => BerserkerTree.Nodes;

        private static class WarriorTree
        {
            public const float CleaveWeaponPct = 0.04f;
            public const int HasteCooldownMs = 1000;
            public const int WillLockoutMs = 60;
            public const float FrustrationKeep = 0.04f;
            public const int EverlastingMs = 500;
            public const float MendingHeal = 0.03f;
            public const int AegisDefense = 2;
            public const int AegisDefenseMs = 6000;
            public const int CastleMaxHealth = 10;
            public const int CastleMaxHealthHigh = 5;
            public const int CastleMaxHealthMs = 6000;
            public const int CastleRageChunk = 50;

            public static readonly NodeDef[] Nodes =
            {
                N("Cleave", "Warrior/Cleave", EffectStyle.Power, r => $"+{Pct(CleaveWeaponPct, r)} weapon damage on ability pulse"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(HasteCooldownMs, r)} ability cooldown"),
                N("Will", "Warrior/Will", EffectStyle.Power, r => WillLine(WillLockoutMs, r, ExtraPulsesAt(r))),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(FrustrationKeep, r)} rage after ability use"),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(EverlastingMs, r)} ability duration"),
                N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Pct(MendingHeal, r)} ability heal"),
                N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => OnUseStat(AegisDefense, AegisDefenseMs, r, "Defense")),
                N("Castle", "Warrior/Castle", EffectStyle.Defense, r => $"+{CastleHealth(r)} Max Health per {CastleRageChunk} rage for {CastleMaxHealthMs / 1000f:0.#}s"),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.weaponDamagePct = Sc(CleaveWeaponPct, r[0]);
                snap.cooldownFlatMs = Sc(HasteCooldownMs, r[1]);
                snap.pulseLockoutMs = SkillTreeFunctions.Base_Pulse_Lockout_Ms - Sc(WillLockoutMs, r[2]);
                snap.rageKeep = Sc(FrustrationKeep, r[3]);
                snap.durationBonusMs = Sc(EverlastingMs, r[4]);
                snap.healPower = Sc(MendingHeal, r[5]);
                SetOnUse(ref snap.hymnDefense, ref snap.hymnDefenseMs, AegisDefense, AegisDefenseMs, r[6]);
                snap.hymnMaxHealth = CastleHealth(r[7]);
                snap.hymnMaxHealthMs = MsIf(CastleMaxHealthMs, r[7]);
                snap.hymnMaxHealthRageChunk = CastleRageChunk;
            }

            public static int CastleHealth(int rank)
            {
                rank = Math.Max(0, rank);
                if (rank <= 3)
                    return CastleMaxHealth * rank;
                return CastleMaxHealth * 3 + CastleMaxHealthHigh * (rank - 3);
            }

            private static int ExtraPulsesAt(int rank)
            {
                int duration = (int)AbilityFunctions.Warrior.GetAbilityDuration(100);
                int faster = Sc(WillLockoutMs, rank);
                int lockout = ClampLockout(SkillTreeFunctions.Base_Pulse_Lockout_Ms, 1, faster);
                return ExtraPulses(duration, SkillTreeFunctions.Base_Pulse_Lockout_Ms, lockout, true);
            }
        }

        private static class RangerTree
        {
            public const float WrathDamagePct = 0.15f;
            public const float ManifestRadius = 0.8f;
            public const int EverlastingMs = 80;
            public const float FrustrationKeep = 0.08f;
            public const float HasteCooldownPct = 0.05f;
            public const float UnfurlRange = 1f;
            public const int EnigmaSlowMs = 400;
            public const int GriefRageOnKill = 4;

            public static readonly NodeDef[] Nodes =
            {
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(WrathDamagePct, r)} ability damage"),
                N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Sc(ManifestRadius, r):0.#} tile ability radius"),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sc(EverlastingMs, r)} ms ability duration"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(FrustrationKeep, r)} rage after ability use"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(HasteCooldownPct, r)} ability cooldown"),
                N("Unfurl", "Ranger/Unfurl", EffectStyle.Power, r => $"+{Sc(UnfurlRange, r)} tile ability range"),
                N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(EnigmaSlowMs, r)}"),
                N("Grief", "Ranger/Grief", EffectStyle.Power, r => $"+{Sc(GriefRageOnKill, r)} rage on ability kill"),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.abilityDamagePct = Sc(WrathDamagePct, r[0]);
                snap.abilityRadiusBonus = Sc(ManifestRadius, r[1]);
                snap.durationBonusMs = Sc(EverlastingMs, r[2]);
                snap.rageKeep = Sc(FrustrationKeep, r[3]);
                snap.cooldownMul = MulMinus(HasteCooldownPct, r[4]);
                snap.abilityRangeBonus = Sc(UnfurlRange, r[5]);
                snap.slowMs = Sc(EnigmaSlowMs, r[6]);
                snap.rageOnKill = Sc(GriefRageOnKill, r[7]);
            }
        }

        private static class LancerTree
        {
            public const float WrathDamagePct = 0.08f;
            public const float FrustrationRageCost = 0.5f;
            public const float ManifestSizePct = 0.10f;
            public const int GriefRageOnKill = 3;
            public const int HasteCooldownMs = 8;
            public const float AttunedWobblePct = 0.10f;
            public const int BlightAttack = 2;
            public const int BlightMs = 6000;

            public static readonly NodeDef[] Nodes =
            {
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(WrathDamagePct, r)} ability damage"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"-{Sc(FrustrationRageCost, r):0.#} rage cost"),
                N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Pct(ManifestSizePct, r)} ability size"),
                N("Grief", "Ranger/Grief", EffectStyle.Power, r => $"+{Sc(GriefRageOnKill, r)} rage on ability kill"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Sc(HasteCooldownMs, r)} ms ability cooldown"),
                N("Attuned", "Lancer/Attuned", EffectStyle.Power, r => $"-{Pct(AttunedWobblePct, r)} angle wobble"),
                N("Piercing", "Lancer/Piercing", EffectStyle.Power, r => $"+{r / 2} pierce"),
                N("Blight", "Lancer/Blight", EffectStyle.Power, r => OnUseStat(BlightAttack, BlightMs, r, "Attack")),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.abilityDamagePct = Sc(WrathDamagePct, r[0]);
                snap.rageCostFlat = Sc(FrustrationRageCost, r[1]);
                snap.projectileSizePct = Sc(ManifestSizePct, r[2]);
                snap.rageOnKill = Sc(GriefRageOnKill, r[3]);
                snap.cooldownFlatMs = Sc(HasteCooldownMs, r[4]);
                snap.wobbleMul = MulMinus(AttunedWobblePct, r[5]);
                snap.pierce = r[6] / 2;
                SetOnUse(ref snap.timedAttack, ref snap.timedAttackMs, BlightAttack, BlightMs, r[7]);
            }
        }

        private static class BladeweaverTree
        {
            public const float WrathDamagePct = 0.03f;
            public const float UnfurlRange = 0.4f;
            public const int EverlastingMs = 20;
            public const float FrustrationKeep = 0.06f;
            public const int HasteCooldownMs = 1000;
            public const float ManifestSizePct = 0.15f;
            public const int AlacritySpeed = 2;
            public const int AlacrityMs = 6000;
            public const int AegisInvulnMs = 150;

            public static readonly NodeDef[] Nodes =
            {
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(WrathDamagePct, r)} ability damage"),
                N("Unfurl", "Bladeweaver/Unfurl", EffectStyle.Agility, r => $"+{Sc(UnfurlRange, r):0.#} tile ability range"),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sc(EverlastingMs, r)} ms ability duration"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(FrustrationKeep, r)} rage after ability hit"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(HasteCooldownMs, r)} ability cooldown"),
                N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Pct(ManifestSizePct, r)} ability size"),
                N("Alacrity", "Bladeweaver/Alacrity", EffectStyle.Agility, r => OnUseStat(AlacritySpeed, AlacrityMs, r, "Speed")),
                N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{Sec(AegisInvulnMs, r)} Invulnerable after ability ends"),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.abilityDamagePct = Sc(WrathDamagePct, r[0]);
                snap.abilityRangeBonus = Sc(UnfurlRange, r[1]);
                snap.durationBonusMs = Sc(EverlastingMs, r[2]);
                snap.rageKeep = Sc(FrustrationKeep, r[3]);
                snap.cooldownFlatMs = Sc(HasteCooldownMs, r[4]);
                snap.projectileSizePct = Sc(ManifestSizePct, r[5]);
                SetOnUse(ref snap.speedOnHit, ref snap.speedOnHitMs, AlacritySpeed, AlacrityMs, r[6]);
                snap.postDashInvulnMs = Sc(AegisInvulnMs, r[7]);
            }
        }

        private static class NomadTree
        {
            public const float WrathMarkedDamagePct = 0.08f;
            public const int FrustrationMarkedRage = 1;
            public const int MendingInteractHeal = 10;
            public const int EverlastingMs = 2000;
            public const float HasteCooldownPct = 0.08f;
            public const float ManifestRadius = 0.3f;
            public const int FlickerRofMs = 500;
            public const int ResonateLingerMs = 1000;

            public static readonly NodeDef[] Nodes =
            {
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(WrathMarkedDamagePct, r)} damage to Marked enemies"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"+{Sc(FrustrationMarkedRage, r)} rage when hitting Marked enemies"),
                N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Sc(MendingInteractHeal, r)} heal on charm interact"),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SecI(EverlastingMs, r)} ability duration"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(HasteCooldownPct, r)} ability cooldown"),
                N("Manifest", "Nomad/ManifestFocus", EffectStyle.Focus, r => $"+{Sc(ManifestRadius, r):0.##} tile ability radius"),
                N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{Sec(FlickerRofMs, r)} Rate of Fire duration"),
                N("Resonate", "Nomad/Resonate", EffectStyle.Focus, r => $"+{Sec(ResonateLingerMs, r)} Marked duration"),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.markedDamagePct = Sc(WrathMarkedDamagePct, r[0]);
                snap.markedRage = Sc(FrustrationMarkedRage, r[1]);
                snap.interactHealBonus = Sc(MendingInteractHeal, r[2]);
                snap.durationBonusMs = Sc(EverlastingMs, r[3]);
                snap.cooldownMul = MulMinus(HasteCooldownPct, r[4]);
                snap.markRadiusBonus = Sc(ManifestRadius, r[5]);
                snap.rofDurationBonusMs = Sc(FlickerRofMs, r[6]);
                snap.markedLingerMs = Sc(ResonateLingerMs, r[7]);
            }
        }

        private static class BrewerTree
        {
            public const int EverlastingMs = 500;
            public const float ManifestRadius = 0.6f;
            public const int FlickerRof = 2;
            public const int MendingVigor = 2;
            public const float HasteCooldownPct = 0.05f;
            public const float FrustrationKeep = 0.08f;
            public const int EnigmaSlowMs = 800;
            public const int AegisDefense = 2;
            public const int AegisDefenseMs = 6000;

            public static readonly NodeDef[] Nodes =
            {
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(EverlastingMs, r)} ability duration"),
                N("Manifest", "Brewer/ManifestSupport", EffectStyle.Support, r => $"+{Sc(ManifestRadius, r):0.#} tile ability radius"),
                N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => OnUseStatRagePct(FlickerRof, r, "RoF after drinking purple brew")),
                N("Mending", "Warrior/Mending", EffectStyle.Support, r => OnUseStatRage(MendingVigor, r, "Vigor after drinking red brew")),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(HasteCooldownPct, r)} ability cooldown"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(FrustrationKeep, r)} rage after ability use"),
                N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(EnigmaSlowMs, r)}"),
                N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => OnUseStat(AegisDefense, AegisDefenseMs, r, "Defense")),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.durationBonusMs = Sc(EverlastingMs, r[0]);
                snap.abilityRadiusBonus = Sc(ManifestRadius, r[1]);
                snap.rofAmount = OnUse(FlickerRof, r[2]);
                snap.vigorBonus = OnUse(MendingVigor, r[3]);
                snap.cooldownMul = MulMinus(HasteCooldownPct, r[4]);
                snap.rageKeep = Sc(FrustrationKeep, r[5]);
                snap.slowMs = Sc(EnigmaSlowMs, r[6]);
                snap.hymnDefense = OnUse(AegisDefense, r[7]);
                snap.timedDefenseMs = MsIf(AegisDefenseMs, r[7]);
            }
        }

        private static class CommanderTree
        {
            public const float EverlastingDurationPct = 0.08f;
            public const float UnfurlDurationPct = 0.15f;
            public const int BrandishMs = 500;
            public const int BlightAttack = 2;
            public const int BlightMs = 6000;
            public const int HasteCooldownMs = 1000;
            public const float FrustrationKeep = 0.08f;
            public const int AegisDefense = 2;
            public const int AegisDefenseMs = 6000;
            public const int CastleBlockChancePct = 1;
            public const int CastleBlockChanceMs = 6000;

            public static readonly NodeDef[] Nodes =
            {
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Pct(EverlastingDurationPct, r)} ability duration"),
                N("Unfurl", "Bladeweaver/Unfurl", EffectStyle.Agility, r => $"+{Pct(UnfurlDurationPct, r)} Reach duration"),
                N("Brandish", "Commander/Brandish", EffectStyle.Agility, r => $"+{Sec(BrandishMs, r)} range duration"),
                N("Blight", "Lancer/Blight", EffectStyle.Power, r => OnUseStat(BlightAttack, BlightMs, r, "Attack")),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(HasteCooldownMs, r)} ability cooldown"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(FrustrationKeep, r)} rage after ability use"),
                N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => OnUseStat(AegisDefense, AegisDefenseMs, r, "Defense")),
                N("Castle", "Warrior/Castle", EffectStyle.Defense, r => OnUseStatPct(CastleBlockChancePct, CastleBlockChanceMs, r, "Block Chance")),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.durationMul = MulPlus(EverlastingDurationPct, r[0]);
                snap.abilityRangeBonus = Sc(UnfurlDurationPct, r[1]);
                snap.durationBonusMs = Sc(BrandishMs, r[2]);
                SetOnUse(ref snap.timedAttack, ref snap.timedAttackMs, BlightAttack, BlightMs, r[3]);
                snap.cooldownFlatMs = Sc(HasteCooldownMs, r[4]);
                snap.rageKeep = Sc(FrustrationKeep, r[5]);
                SetOnUse(ref snap.hymnDefense, ref snap.hymnDefenseMs, AegisDefense, AegisDefenseMs, r[6]);
                SetOnUse(ref snap.hymnBlockChance, ref snap.hymnBlockChanceMs, CastleBlockChancePct, CastleBlockChanceMs, r[7]);
            }
        }

        private static class MinisterTree
        {
            public const float MendingHeal = 0.08f;
            public const float ManifestRadius = 0.5f;
            public const int BasePulseLockoutMs = 2000;
            public const int MinPulseLockoutMs = 400;
            public const int WillLockoutMs = 150;
            public const int EverlastingMs = 1000;
            public const float HasteCooldownPct = 0.05f;
            public const int PurifyAbsorptionChance = 1;
            public const int PurifyAbsorptionMs = 6000;
            public const int PurifyRageChunk = 50;
            public const int WrathAttack = 2;
            public const int WrathAttackMs = 6000;
            public const int AegisDefense = 2;
            public const int AegisDefenseMs = 6000;

            public static readonly NodeDef[] Nodes =
            {
                N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Pct(MendingHeal, r)} ability heal"),
                N("Manifest", "Brewer/ManifestSupport", EffectStyle.Support, r => $"+{Sc(ManifestRadius, r):0.#} tile ability radius"),
                N("Will", "Warrior/Will", EffectStyle.Agility, r => WillLine(WillLockoutMs, r, ExtraPulsesAt(r))),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(EverlastingMs, r)} ability duration"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(HasteCooldownPct, r)} ability cooldown"),
                N("Purify", "Brewer/Purify", EffectStyle.Support, r => OnUseStatPct(PurifyAbsorptionChance, PurifyAbsorptionMs, r, "Absorption Chance", PurifyRageChunk)),
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => OnUseStat(WrathAttack, WrathAttackMs, r, "Attack")),
                N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => OnUseStat(AegisDefense, AegisDefenseMs, r, "Defense")),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.healPower = Sc(MendingHeal, r[0]);
                snap.abilityRadiusBonus = Sc(ManifestRadius, r[1]);
                snap.pulseLockoutMs = BasePulseLockoutMs - Sc(WillLockoutMs, r[2]);
                snap.durationBonusMs = Sc(EverlastingMs, r[3]);
                snap.cooldownMul = MulMinus(HasteCooldownPct, r[4]);
                SetOnUse(ref snap.absorptionChance, ref snap.absorptionChanceMs, PurifyAbsorptionChance, PurifyAbsorptionMs, r[5]);
                snap.absorptionRageChunk = PurifyRageChunk;
                SetOnUse(ref snap.timedAttack, ref snap.timedAttackMs, WrathAttack, WrathAttackMs, r[6]);
                SetOnUse(ref snap.fieldDefense, ref snap.fieldDefenseMs, AegisDefense, AegisDefenseMs, r[7]);
            }

            private static int ExtraPulsesAt(int rank)
            {
                int duration = AbilityFunctions.Minister.GetPillarDurationMs(100);
                int faster = Sc(WillLockoutMs, rank);
                int lockout = ClampLockout(BasePulseLockoutMs, MinPulseLockoutMs, faster);
                return ExtraPulses(duration, BasePulseLockoutMs, lockout, false);
            }
        }

        private static class AlchemistTree
        {
            public const float WrathDamagePct = 0.20f;
            public const float ManifestRadius = 0.5f;
            public const int BasePulseLockoutMs = 1000;
            public const int MinPulseLockoutMs = 200;
            public const int WillLockoutMs = 60;
            public const float EverlastingDurationPct = 0.10f;
            public const float HasteCooldownPct = 0.05f;
            public const float FrustrationKeep = 0.08f;
            public const int BlightAttack = 2;
            public const int BlightMs = 6000;
            public const int EnigmaSlowMs = 300;

            public static readonly NodeDef[] Nodes =
            {
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(WrathDamagePct, r)} ability damage"),
                N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Sc(ManifestRadius, r):0.#} tile ability radius"),
                N("Will", "Warrior/Will", EffectStyle.Agility, r => WillLine(WillLockoutMs, r, ExtraPulsesAt(r))),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Pct(EverlastingDurationPct, r)} ability duration"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(HasteCooldownPct, r)} ability cooldown"),
                N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(FrustrationKeep, r)} rage after ability use"),
                N("Blight", "Lancer/Blight", EffectStyle.Power, r => OnUseStat(BlightAttack, BlightMs, r, "Attack")),
                N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(EnigmaSlowMs, r)}"),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.abilityDamagePct = Sc(WrathDamagePct, r[0]);
                snap.abilityRadiusBonus = Sc(ManifestRadius, r[1]);
                snap.pulseLockoutMs = BasePulseLockoutMs - Sc(WillLockoutMs, r[2]);
                snap.durationMul = MulPlus(EverlastingDurationPct, r[3]);
                snap.cooldownMul = MulMinus(HasteCooldownPct, r[4]);
                snap.rageKeep = Sc(FrustrationKeep, r[5]);
                SetOnUse(ref snap.timedAttack, ref snap.timedAttackMs, BlightAttack, BlightMs, r[6]);
                snap.slowMs = Sc(EnigmaSlowMs, r[7]);
            }

            private static int ExtraPulsesAt(int rank)
            {
                int duration = AbilityFunctions.Alchemist.GetGroundDurationMs(100);
                int faster = Sc(WillLockoutMs, rank);
                int lockout = ClampLockout(BasePulseLockoutMs, MinPulseLockoutMs, faster);
                return ExtraPulses(duration, BasePulseLockoutMs, lockout, false);
            }
        }

        private static class BerserkerTree
        {
            public const float WrathDamagePct = 0.20f;
            public const float UnfurlRange = 0.6f;
            public const float ManifestSpreadDeg = 6f;
            public const int EnigmaSlowMs = 600;
            public const float HasteCooldownPct = 0.05f;
            public const int EverlastingRofMs = 500;
            public const int FlickerRof = 2;
            public const int BlightAttack = 2;
            public const int BlightMs = 6000;

            public static readonly NodeDef[] Nodes =
            {
                N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(WrathDamagePct, r)} ability damage"),
                N("Unfurl", "Ranger/Unfurl", EffectStyle.Power, r => $"+{Sc(UnfurlRange, r):0.#} tile ability range"),
                N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Sc(ManifestSpreadDeg, r)}\u00b0 ability spread"),
                N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"+{Sec(EnigmaSlowMs, r)} Slowed"),
                N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(HasteCooldownPct, r)} ability cooldown"),
                N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(EverlastingRofMs, r)} Rate of Fire duration"),
                N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => OnUseStatRagePct(FlickerRof, r, "Rate of Fire")),
                N("Blight", "Lancer/Blight", EffectStyle.Power, r => OnUseStat(BlightAttack, BlightMs, r, "Attack")),
            };

            public static void Apply(int[] r, ref AbilityModifierSnapshot snap)
            {
                snap.abilityDamagePct = Sc(WrathDamagePct, r[0]);
                snap.abilityRangeBonus = Sc(UnfurlRange, r[1]);
                snap.shoutSpreadDeg = Sc(ManifestSpreadDeg, r[2]);
                snap.slowMs = Sc(EnigmaSlowMs, r[3]);
                snap.cooldownMul = MulMinus(HasteCooldownPct, r[4]);
                snap.durationBonusMs = Sc(EverlastingRofMs, r[5]);
                snap.rofAmount = OnUse(FlickerRof, r[6]);
                SetOnUse(ref snap.timedAttack, ref snap.timedAttackMs, BlightAttack, BlightMs, r[7]);
            }
        }

        private static NodeDef N(string name, string sprite, EffectStyle style, Func<int, string> effect)
        {
            return new NodeDef { name = name, sprite = sprite, style = style, effect = effect };
        }

        private static float Sc(float perRank, int rank) => SkillTreeFunctions.Scale(perRank, rank);
        private static int Sc(int perRank, int rank) => SkillTreeFunctions.Scale(perRank, rank);
        private static int OnUse(int perRank, int rank) => SkillTreeFunctions.ScaleOnUseRank(perRank, rank);
        private static int MsIf(int durationMs, int rank) => rank > 0 ? durationMs : 0;
        private static float MulMinus(float perRank, int rank) => 1f - Sc(perRank, rank);
        private static float MulPlus(float perRank, int rank) => 1f + Sc(perRank, rank);

        private static void SetOnUse(ref int amount, ref int durationMs, int perRank, int ms, int rank)
        {
            amount = OnUse(perRank, rank);
            durationMs = MsIf(ms, rank);
        }

        private static int CountPulses(int durationMs, int lockoutMs, bool pulseAtEnd)
        {
            if (durationMs <= 0 || lockoutMs < 1)
                return 0;
            if (pulseAtEnd)
                return 1 + durationMs / lockoutMs;
            return 1 + (durationMs - 1) / lockoutMs;
        }

        private static int ExtraPulses(int durationMs, int baseLockoutMs, int lockoutMs, bool pulseAtEnd)
        {
            int now = CountPulses(durationMs, lockoutMs, pulseAtEnd);
            int baseline = CountPulses(durationMs, baseLockoutMs, pulseAtEnd);
            return Math.Max(0, now - baseline);
        }

        private static int ClampLockout(int baseLockoutMs, int minLockoutMs, int msFaster)
        {
            return Math.Max(minLockoutMs, baseLockoutMs - msFaster);
        }

        private static string WillLine(int msPerRank, int rank, int extraPulses)
        {
            int ms = Sc(msPerRank, rank);
            string pulseWord = extraPulses == 1 ? "pulse" : "pulses";
            return $"-{ms}ms ability lockout ({extraPulses} extra {pulseWord})";
        }

        private static string OnUseStat(int perRank, int durationMs, int r, string stat)
        {
            return $"+{OnUse(perRank, r)} {stat} per {SkillTreeFunctions.On_Use_Stat_Rage_Chunk} rage for {durationMs / 1000f:0.#}s";
        }

        private static string OnUseStatPct(int perRank, int durationMs, int r, string stat)
        {
            return OnUseStatPct(perRank, durationMs, r, stat, SkillTreeFunctions.On_Use_Stat_Rage_Chunk);
        }

        private static string OnUseStatPct(int perRank, int durationMs, int r, string stat, int rageChunk)
        {
            return $"+{OnUse(perRank, r)}% {stat} per {rageChunk} rage for {durationMs / 1000f:0.#}s";
        }

        private static string OnUseStatRage(int perRank, int r, string stat)
        {
            return $"+{OnUse(perRank, r)} {stat} per {SkillTreeFunctions.On_Use_Stat_Rage_Chunk} rage";
        }

        private static string OnUseStatRagePct(int perRank, int r, string stat)
        {
            return $"+{OnUse(perRank, r)}% {stat} per {SkillTreeFunctions.On_Use_Stat_Rage_Chunk} rage";
        }

        private static string Pct(float perRank, int r) => $"{Sc(perRank, r) * 100:0}%";

        private static string Sec(int msPerRank, int r)
        {
            float s = Sc(msPerRank, r) / 1000f;
            return $"{s:0.##}s";
        }

        private static string SecI(int msPerRank, int r)
        {
            float s = Sc(msPerRank, r) / 1000f;
            if (Math.Abs(s - Math.Round(s)) < 0.001f)
                return $"{(int)Math.Round(s)}s";
            return $"{s:0.##}s";
        }
    }
}
