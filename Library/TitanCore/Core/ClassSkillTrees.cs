using System;

namespace TitanCore.Core
{
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
                    snap.weaponDamagePct = SkillTreeFunctions.Scale(0.04f, r[0]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(1000, r[1]);
                    snap.pulseLockoutMs = SkillTreeFunctions.Base_Pulse_Lockout_Ms - SkillTreeFunctions.Scale(50, r[2]);
                    snap.rageKeep = SkillTreeFunctions.Scale(0.04f, r[3]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(500, r[4]);
                    snap.healPower = SkillTreeFunctions.Scale(0.03f, r[5]);
                    snap.hymnDefense = SkillTreeFunctions.Scale(3, r[6]);
                    snap.hymnMaxHealth = SkillTreeFunctions.Scale(5, r[7]);
                    break;
                case ClassType.Ranger:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(0.12f, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(0.4f, r[1]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(80, r[2]);
                    snap.rageKeep = SkillTreeFunctions.Scale(0.08f, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(0.05f, r[4]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(1f, r[5]);
                    snap.slowMs = SkillTreeFunctions.Scale(400, r[6]);
                    snap.rageOnKill = SkillTreeFunctions.Scale(2, r[7]);
                    break;
                case ClassType.Lancer:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(0.08f, r[0]);
                    snap.rageCostFlat = SkillTreeFunctions.Scale(0.5f, r[1]);
                    snap.projectileSizePct = SkillTreeFunctions.Scale(0.06f, r[2]);
                    snap.rageOnKill = SkillTreeFunctions.Scale(2, r[3]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(8, r[4]);
                    snap.wobbleMul = 1f - SkillTreeFunctions.Scale(0.10f, r[5]);
                    snap.pierce = r[6] / 2;
                    snap.timedAttack = SkillTreeFunctions.Scale(2, r[7]);
                    snap.timedAttackMs = r[7] > 0 ? 600 : 0;
                    break;
                case ClassType.Bladeweaver:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(0.03f, r[0]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(0.4f, r[1]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(20, r[2]);
                    snap.rageKeep = SkillTreeFunctions.Scale(0.06f, r[3]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(1000, r[4]);
                    snap.projectileSizePct = SkillTreeFunctions.Scale(0.08f, r[5]);
                    snap.speedOnHitMs = r[6] > 0 ? SkillTreeFunctions.Scale(300, r[6]) : 0;
                    snap.speedOnHit = r[6] > 0 ? 2 : 0;
                    snap.postDashInvulnMs = SkillTreeFunctions.Scale(150, r[7]);
                    break;
                case ClassType.Nomad:
                    snap.markedDamagePct = SkillTreeFunctions.Scale(0.08f, r[0]);
                    snap.markedRage = SkillTreeFunctions.Scale(1, r[1]);
                    snap.interactHealBonus = SkillTreeFunctions.Scale(10, r[2]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(2000, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(0.08f, r[4]);
                    snap.markRadiusBonus = SkillTreeFunctions.Scale(0.25f, r[5]);
                    snap.rofDurationBonusMs = SkillTreeFunctions.Scale(500, r[6]);
                    snap.markedLingerMs = SkillTreeFunctions.Scale(500, r[7]);
                    break;
                case ClassType.Brewer:
                    snap.durationBonusMs = SkillTreeFunctions.Scale(500, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(0.4f, r[1]);
                    snap.rofAmount = SkillTreeFunctions.Scale(2, r[2]);
                    snap.vigorBonus = SkillTreeFunctions.Scale(2, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(0.05f, r[4]);
                    snap.rageKeep = SkillTreeFunctions.Scale(0.08f, r[5]);
                    snap.slowMs = SkillTreeFunctions.Scale(800, r[6]);
                    snap.hymnDefense = SkillTreeFunctions.Scale(3, r[7]);
                    snap.timedDefenseMs = r[7] > 0 ? 2000 : 0;
                    break;
                case ClassType.Commander:
                    snap.durationMul = 1f + SkillTreeFunctions.Scale(0.08f, r[0]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(0.3f, r[1]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(500, r[2]);
                    snap.pulseLockoutMs = 1000 - SkillTreeFunctions.Scale(50, r[3]);
                    snap.cooldownFlatMs = SkillTreeFunctions.Scale(1000, r[4]);
                    snap.rageKeep = SkillTreeFunctions.Scale(0.08f, r[5]);
                    snap.hymnDefense = SkillTreeFunctions.Scale(2, r[6]);
                    snap.hymnMaxHealth = SkillTreeFunctions.Scale(10, r[7]);
                    break;
                case ClassType.Minister:
                    snap.healPower = SkillTreeFunctions.Scale(0.08f, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(0.4f, r[1]);
                    snap.pulseLockoutMs = 2000 - SkillTreeFunctions.Scale(100, r[2]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(500, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(0.05f, r[4]);
                    snap.vigorBonus = SkillTreeFunctions.Scale(2, r[5]);
                    snap.timedAttack = SkillTreeFunctions.Scale(2, r[6]);
                    snap.fieldDefense = SkillTreeFunctions.Scale(2, r[7]);
                    break;
                case ClassType.Alchemist:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(0.10f, r[0]);
                    snap.abilityRadiusBonus = SkillTreeFunctions.Scale(0.4f, r[1]);
                    snap.pulseLockoutMs = 1000 - SkillTreeFunctions.Scale(50, r[2]);
                    snap.durationMul = 1f + SkillTreeFunctions.Scale(0.08f, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(0.05f, r[4]);
                    snap.rageKeep = SkillTreeFunctions.Scale(0.08f, r[5]);
                    snap.timedAttack = SkillTreeFunctions.Scale(1, r[6]);
                    snap.slowMs = SkillTreeFunctions.Scale(300, r[7]);
                    break;
                case ClassType.Berserker:
                    snap.abilityDamagePct = SkillTreeFunctions.Scale(0.12f, r[0]);
                    snap.abilityRangeBonus = SkillTreeFunctions.Scale(0.5f, r[1]);
                    snap.shoutSpreadDeg = SkillTreeFunctions.Scale(4f, r[2]);
                    snap.slowMs = SkillTreeFunctions.Scale(400, r[3]);
                    snap.cooldownMul = 1f - SkillTreeFunctions.Scale(0.05f, r[4]);
                    snap.durationBonusMs = SkillTreeFunctions.Scale(500, r[5]);
                    snap.rofAmount = SkillTreeFunctions.Scale(2, r[6]);
                    snap.timedAttack = SkillTreeFunctions.Scale(3, r[7]);
                    snap.timedAttackMs = r[7] > 0 ? 2000 : 0;
                    break;
            }
        }

        public static readonly NodeDef[] Warrior =
        {
            N("Cleave", "Warrior/Cleave", EffectStyle.Power, r => $"+{Pct(0.04f, r)} weapon damage on ability pulse"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(1000, r)} ability cooldown"),
            N("Will", "Warrior/Will", EffectStyle.Power, r => $"Ability pulses {SkillTreeFunctions.Scale(50, r)} ms more often"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(0.04f, r)} rage after ability use"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(500, r)} ability duration"),
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Pct(0.03f, r)} ability heal"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(3, r)} Defense while ability is active"),
            N("Castle", "Warrior/Castle", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(5, r)} Max Health while ability is active"),
        };

        public static readonly NodeDef[] Ranger =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(0.12f, r)} ability damage"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(0.4f, r):0.#} tile ability radius"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(80, r)} ms ability duration"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(0.08f, r)} rage after ability use"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(0.05f, r)} ability cooldown"),
            N("Unfurl", "Ranger/Unfurl", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(1, r)} tile ability range"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(400, r)}"),
            N("Grief", "Ranger/Grief", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(2, r)} rage on ability kill"),
        };

        public static readonly NodeDef[] Lancer =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(0.08f, r)} ability damage"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"-{SkillTreeFunctions.Scale(0.5f, r):0.#} rage cost"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Pct(0.06f, r)} ability size"),
            N("Grief", "Ranger/Grief", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(2, r)} rage on ability kill"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SkillTreeFunctions.Scale(8, r)} ms ability cooldown"),
            N("Attuned", "Lancer/Attuned", EffectStyle.Power, r => $"-{Pct(0.10f, r)} angle wobble"),
            N("Piercing", "Lancer/Piercing", EffectStyle.Power, r => $"+{r / 2} pierce"),
            N("Blight", "Lancer/Blight", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(2, r)} Attack for 0.6s after ability use"),
        };

        public static readonly NodeDef[] Bladeweaver =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(0.03f, r)} ability damage"),
            N("Unfurl", "Bladeweaver/Unfurl", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(0.4f, r):0.#} tile ability range"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(20, r)} ms ability duration"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(0.06f, r)} rage after ability hit"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(1000, r)} ability cooldown"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{Pct(0.08f, r)} ability size"),
            N("Alacrity", "Bladeweaver/Alacrity", EffectStyle.Agility, r => $"+2 Speed for {Sec(300, r)} on ability hit"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{Sec(150, r)} Invulnerable after ability ends"),
        };

        public static readonly NodeDef[] Nomad =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(0.08f, r)} damage to Marked enemies"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(1, r)} rage when hitting Marked enemies"),
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(10, r)} heal on charm interact"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{SecI(2000, r)} ability duration"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(0.08f, r)} ability cooldown"),
            N("Manifest", "Nomad/ManifestFocus", EffectStyle.Focus, r => $"+{SkillTreeFunctions.Scale(0.25f, r):0.##} tile ability radius"),
            N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{Sec(500, r)} Rate of Fire duration"),
            N("Resonate", "Nomad/Resonate", EffectStyle.Focus, r => $"+{Sec(500, r)} Marked duration"),
        };

        public static readonly NodeDef[] Brewer =
        {
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(500, r)} ability duration"),
            N("Manifest", "Brewer/ManifestSupport", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(0.4f, r):0.#} tile ability radius"),
            N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(2, r)}% RoF after drinking purple brew"),
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(2, r)} Vigor after drinking red brew"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(0.05f, r)} ability cooldown"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(0.08f, r)} rage after ability use"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(800, r)}"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(3, r)} Defense for 2s after ability use"),
        };

        public static readonly NodeDef[] Commander =
        {
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Pct(0.08f, r)} ability duration"),
            N("Unfurl", "Bladeweaver/Unfurl", EffectStyle.Agility, r => $"+{SkillTreeFunctions.Scale(0.3f, r):0.#} tile ability range"),
            N("Brandish", "Commander/Brandish", EffectStyle.Agility, r => $"+{Sec(500, r)} range duration"),
            N("Will", "Warrior/Will", EffectStyle.Power, r => $"Ability pulses {SkillTreeFunctions.Scale(50, r)} ms more often"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{SecI(1000, r)} ability cooldown"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(0.08f, r)} rage after ability use"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(2, r)} Defense while ability is active"),
            N("Castle", "Warrior/Castle", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(10, r)} Max Health while ability is active"),
        };

        public static readonly NodeDef[] Minister =
        {
            N("Mending", "Warrior/Mending", EffectStyle.Support, r => $"+{Pct(0.08f, r)} ability heal"),
            N("Manifest", "Brewer/ManifestSupport", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(0.4f, r):0.#} tile ability radius"),
            N("Will", "Warrior/Will", EffectStyle.Agility, r => $"Ability heals {SkillTreeFunctions.Scale(100, r)} ms more often"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(500, r)} ability duration"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(0.05f, r)} ability cooldown"),
            N("Purify", "Brewer/Purify", EffectStyle.Support, r => $"+{SkillTreeFunctions.Scale(2, r)} Vigor while ability is active"),
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(2, r)} Attack while ability is active"),
            N("Aegis", "Warrior/Aegis", EffectStyle.Defense, r => $"+{SkillTreeFunctions.Scale(2, r)} Defense while ability is active"),
        };

        public static readonly NodeDef[] Alchemist =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(0.10f, r)} ability damage"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(0.4f, r):0.#} tile ability radius"),
            N("Will", "Warrior/Will", EffectStyle.Agility, r => $"Ability ticks {SkillTreeFunctions.Scale(50, r)} ms more often"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Pct(0.08f, r)} ability duration"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(0.05f, r)} ability cooldown"),
            N("Frustration", "Warrior/Frustration", EffectStyle.Power, r => $"Keep {Pct(0.08f, r)} rage after ability use"),
            N("Blight", "Lancer/Blight", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(1, r)} Attack while ability is active"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"Ability applies Slowed {Sec(300, r)}"),
        };

        public static readonly NodeDef[] Berserker =
        {
            N("Wrath", "Ranger/Wrath", EffectStyle.Power, r => $"+{Pct(0.12f, r)} ability damage"),
            N("Unfurl", "Ranger/Unfurl", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(0.5f, r):0.#} tile ability range"),
            N("Manifest", "Ranger/ManifestPower", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(4, r)}\u00b0 ability spread"),
            N("Enigma", "Ranger/Enigma", EffectStyle.Focus, r => $"+{Sec(400, r)} Slowed"),
            N("Haste", "Warrior/Haste", EffectStyle.Agility, r => $"-{Pct(0.05f, r)} ability cooldown"),
            N("Everlasting", "Warrior/Everlasting", EffectStyle.Agility, r => $"+{Sec(500, r)} Rate of Fire duration"),
            N("Flicker", "Nomad/Flicker", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(2, r)}% Rate of Fire"),
            N("Blight", "Lancer/Blight", EffectStyle.Power, r => $"+{SkillTreeFunctions.Scale(3, r)} Attack for 2s after ability use"),
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
