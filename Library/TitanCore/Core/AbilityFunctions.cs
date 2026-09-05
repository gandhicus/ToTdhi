using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data.Components;
using TitanCore.Data.Entities;
using Utils.NET.Geometry;

namespace TitanCore.Core
{
    public static class AbilityFunctions
    {
        public struct AbilityEffect
        {
            public StatusEffect type;

            public uint duration;

            public float area;

            public AbilityEffect(StatusEffect type, uint duration, float area)
            {
                this.type = type;
                this.duration = duration;
                this.area = area;
            }
        }

        public static int GetAbilityCooldownMs(byte rage, ushort classId)
        {
            switch ((ClassType)classId)
            {
                case ClassType.Ranger:
                    return 10000;
                case ClassType.Warrior:
                    return 20000;
                case ClassType.Commander:
                    return 10000;
                case ClassType.Lancer:
                    return 1000;
                case ClassType.Alchemist:
                    return 10000;
                case ClassType.Minister:
                    return 10000;
                case ClassType.Berserker:
                    return 10000;
                case ClassType.Brewer:
                    return 4000;
                case ClassType.Bladeweaver:
                    return 1000;
                case ClassType.Nomad:
                    return 4000;
                default:
                    return int.MaxValue;
            }
        }

        public static List<AbilityEffect> GetAbilityEffects(byte rage, int attack, byte value, ClassType classType)
        {
            return new List<AbilityEffect>();
        }

        public static class Commander
        {
            public const int BasePulseLockoutMs = 2500;
            public const int MinPulseLockoutMs = 1200;
            public const int PulseDefense = 3;
            public const int MaxPulseStacks = 5;

            public static int GetDefenseDurationMs(int rage, int attack)
            {
                float rageScalar = rage / 100f;
                float attackScalar = 0.5f + attack / 50f;
                return (int)(1000 + 7000 * rageScalar * attackScalar);
            }
        }

        public static class Alchemist
        {
            public const float Air_Time = 1;

            // 0.2x one elixir shot per tick at 100 rage. The puddle ticks about 11 times
            // at default 1s spacing (more with Will), so this stays well below a Ranger rain.
            public const float Weapon_Damage_Mul = 0.2f;

            public const float Rage_Damage_At_100 = 1f;

            public const int Puddle_Attack = 4;

            public const uint Puddle_Attack_Ms = 1050;

            public static int GetGroundDurationMs(byte rage)
            {
                return 1000 + (int)((rage / 100.0f) * 10000);
            }

            public static float GetRadius(byte rage)
            {
                return 6;
            }

            public static int ScaleWeaponDamage(int weaponShotDamage)
            {
                return Math.Max(1, (int)(weaponShotDamage * Weapon_Damage_Mul));
            }
        }

        public static class Lancer
        {
            public const int Rage_Cost = 25;

            public const ushort Ability_Item_Id = 0x2a1;

            public const float Weapon_Damage_Mul = 2f;

            public const int Nova_Count = 12;

            public const int Nova_Hits_Per_Target = 3;

            public static IEnumerable<float> GetNovaAngles(float aimAngle)
            {
                float step = (float)Math.PI * 2f / Nova_Count;
                for (int i = 0; i < Nova_Count; i++)
                    yield return aimAngle + i * step;
            }

            public static float GetProjectileSize(int rage)
            {
                return 1f + (rage / 100f);
            }

            public static int ScaleWeaponDamage(int weaponShotDamage)
            {
                return Math.Max(1, (int)(weaponShotDamage * Weapon_Damage_Mul));
            }

            public static bool RollsPierce(int pierceChancePercent, uint projectileId, uint ownerGameId)
            {
                if (pierceChancePercent <= 0) return false;
                int chance = Math.Min(100, pierceChancePercent);
                return (StatFunctions.GetCombatSeed(projectileId, 0, ownerGameId) % 100u) < (uint)chance;
            }
        }

        public static class Minister
        {
            public static byte GetRageCost(int rage)
            {
                if (rage == 100)
                    return 100;
                else if (rage >= 75)
                    return 75;
                else if (rage >= 50)
                    return 50;
                else
                    return 25;
            }

            public static int GetHealAmount(int rage, int attack)
            {
                return rage + (int)(attack * (rage / 100f));
            }

            public static int GetPillarDurationMs(int rage)
            {
                return 8000;
                //return 1000 + (int)((rage / 100.0f) * 8000);
            }

            public static float GetPillarRadius(int rage)
            {
                return 6;// 1 + (rage / 100.0f) * 5;
            }
        }

        public static class Berserker
        {
            public const int RoF_Amount = 20;

            public const float Weapon_Damage_Mul = 0.3f;

            public const float Rage_Damage_At_100 = 20f / 3f;

            public static int ScaleWeaponDamage(int weaponShotDamage)
            {
                return Math.Max(1, (int)(weaponShotDamage * Weapon_Damage_Mul));
            }

            public static float GetShoutSpread(int rage, int attack)
            {
                return AngleUtils.PI * 0.25f;
            }

            public static float GetShoutRange(int rage, int attack)
            {
                return 8;
            }

            public static AbilityEffect GetShoutEffect(int rage, int attack)
            {
                return new AbilityEffect(StatusEffect.Slowed, 5, 0);
            }

            public static float GetRoFArea(int rage, int attack)
            {
                float rageScalar = rage / 100f;
                float attackScalar = 0.5f + attack / 50f;
                return 2 + 6 * rageScalar * attackScalar;
            }

            public static uint GetRoFDurationMs(int rage, int attack)
            {
                float rageScalar = rage / 100f;
                float attackScalar = 0.5f + attack / 50f;
                return (uint)(500 + 6000 * rageScalar * attackScalar);
            }
        }

        public static class Ranger
        {
            // 10x one arrow at 100 rage (30% Attack). T8 is ~1.3k–2.4k at Attack 40.
            // Uses a single shot so multi-arrow bows do not also inflate rain.
            public const float Weapon_Damage_Mul = 1f;

            public const float Rage_Damage_At_100 = 10f;

            public const float Attack_Scale = 0.3f;

            public static float GetRadius(int rage, int attack)
            {
                return 2 + (rage / 100f) * 4;
            }

            public static float PartialAttackModifier(int attack, bool damaging)
            {
                float full = StatFunctions.AttackModifier(attack, damaging);
                return 1f + (full - 1f) * Attack_Scale;
            }

            // weaponShotDamage already includes full AttackModifier from the shot roll.
            public static int ScaleWeaponDamage(int weaponShotDamage, int attack, bool damaging)
            {
                float full = StatFunctions.AttackModifier(attack, damaging);
                if (full < 0.01f)
                    full = 0.01f;
                float raw = weaponShotDamage / full;
                float partial = PartialAttackModifier(attack, damaging);
                return Math.Max(1, (int)(raw * partial * Weapon_Damage_Mul));
            }

            public static AbilityEffect? GetEffect(int rage, int attack)
            {
                return null;
            }
        }

        public static class BladeWeaver
        {
            public static uint Dash_Duration = 150;

            private static float Max_Dash_Distance = 6;

            public static int Max_Dash_Rage = 25;

            public const float Rage_Charge_Per_Second = 100f;

            public const float Charge_Hold_Timeout_Sec = 3f;

            public static int GetChargedRage(float heldTimeSec, float currentRage, float chargeDurationMul)
            {
                float durationMul = chargeDurationMul > 0f ? chargeDurationMul : 1f;
                int fromHold = (int)(heldTimeSec * Rage_Charge_Per_Second / durationMul);
                int cappedByBar = Math.Max(0, (int)Math.Floor(currentRage));
                return Math.Max(0, Math.Min(Max_Dash_Rage, Math.Min(fromHold, cappedByBar)));
            }

            public static Vec2 GetDashPositionVector(float angle, int rage)
            {
                return GetDashPositionVector(angle, rage, Dash_Duration);
            }

            public static Vec2 GetDashPositionVector(float angle, int rage, uint durationMs)
            {
                float rageScalar = Math.Min(rage / (float)Max_Dash_Rage, 1);
                float duration = Math.Max(1, durationMs);
                var speed = rageScalar * Max_Dash_Distance * (1000f / duration);
                return Vec2.FromAngle(angle) * speed;
            }

            public static Vec2 GetDashPositionVector(float angle, int rage, uint durationMs, float extraDistance)
            {
                float rageScalar = Math.Min(rage / (float)Max_Dash_Rage, 1);
                float duration = Math.Max(1, durationMs);
                var speed = rageScalar * (Max_Dash_Distance + extraDistance) * (1000f / duration);
                return Vec2.FromAngle(angle) * speed;
            }

            public static float GetProjectileSize(int rage)
            {
                return 1f + (rage / Max_Dash_Rage);
            }

            // 3x the sword volley at 25 rage matches the old flat slash on a T8 sword at Attack 60.
            public const float Weapon_Damage_Mul = 3f;

            public static int ScaleWeaponDamage(int weaponVolleyDamage, int rage)
            {
                float rageScalar = Math.Min(1f, Math.Max(0, rage) / (float)Max_Dash_Rage);
                return Math.Max(1, (int)(weaponVolleyDamage * Weapon_Damage_Mul * rageScalar));
            }
        }

        public static class Warrior
        {
            public const int Heal_Area = 6;

            public static int GetHealAmount(byte rage, int attack)
            {
                return (int)(rage * 0.5f + attack * rage / 100f);
            }

            public static uint GetAbilityDuration(byte rage)
            {
                return 10000;
            }

            public static int GetCleaveOutgoing(int minDamage, int maxDamage, int attack, bool damaging, float weaponDamagePct)
            {
                if (weaponDamagePct <= 0) return 0;
                float mid = (minDamage + maxDamage) * 0.5f;
                float outgoing = mid * StatFunctions.AttackModifier(attack, damaging) * weaponDamagePct;
                if (outgoing <= 0) return 0;
                return (int)outgoing;
            }
        }

        public static class Brewer
        {
            public const int RoF_Amount = 10;
        }

        public static class Nomad
        {
            public const int Ability_Cost = 35;

            public const float Charm_Air_Time = 0.7f;

            public const int Marked_Linger_Ms = 4000;

            public const float Marked_Hit_Mul = 1.15f;

            public const int RoF_Amount = 5;

            public const uint RoF_Duration_Ms = 4000;

            public static int GetInteractTalismanRof(AbilityModifierSnapshot mods, float activationRage)
            {
                int extra = 0;
                if (!SkillTreeFunctions.IsEnabled || mods.talismanEffects == null)
                    return extra;
                for (int i = 0; i < mods.talismanEffects.Length; i++)
                {
                    var effect = mods.talismanEffects[i];
                    if (effect == null || effect.trigger != TalismanTrigger.Interact)
                        continue;
                    if (!TalismanEffect.MeetsRageThreshold(activationRage, effect))
                        continue;
                    extra += effect.rofAmount;
                    if (effect.alternateStatBonus != null && effect.alternateStatBonus.statType == AlternateStatType.RateOfFire)
                        extra += effect.alternateStatBonus.amount;
                }
                return extra;
            }

            public static int GetInteractRofAmount(AbilityModifierSnapshot mods, float activationRage)
            {
                return RoF_Amount + GetInteractTalismanRof(mods, activationRage);
            }

            public static int ScaleMarkedDamage(int damageTaken, float wrathPct)
            {
                return (int)(damageTaken * (Marked_Hit_Mul + wrathPct));
            }
        }

        public static class RageSpend
        {
            public static byte GetIntegralRage(float rage)
            {
                return (byte)Math.Min(Math.Floor(rage), 100);
            }

            public static void SpendDumpRage(ref byte rageIntegral, AbilityModifierSnapshot mods, out byte rageCost)
            {
                byte keep = 0;
                if (SkillTreeFunctions.IsEnabled && mods.rageKeep > 0)
                    keep = (byte)Math.Round(rageIntegral * mods.rageKeep);
                rageCost = (byte)Math.Max(0, rageIntegral - keep);
                if (rageCost == 0 && rageIntegral > 0)
                    rageCost = rageIntegral;
                rageIntegral = keep;
            }

            public static float ApplySpend(float rageBefore, byte rageIntegralAfter)
            {
                return StatFunctions.ApplyAbilityRageSpend(rageBefore, GetIntegralRage(rageBefore), rageIntegralAfter);
            }

            public static float SpendDumpRage(float rageBefore, AbilityModifierSnapshot mods, out byte rageCost)
            {
                var integral = GetIntegralRage(rageBefore);
                SpendDumpRage(ref integral, mods, out rageCost);
                return ApplySpend(rageBefore, integral);
            }

            public static float SpendFixedCost(float rageBefore, int cost)
            {
                var integral = GetIntegralRage(rageBefore);
                var after = (byte)Math.Max(0, integral - Math.Max(0, cost));
                return ApplySpend(rageBefore, after);
            }

            public static int GetLancerRageCost(AbilityModifierSnapshot mods)
            {
                int cost = (int)Math.Round(Lancer.Rage_Cost - mods.rageCostFlat);
                return Math.Max(1, cost);
            }

            public const float Damage_Mul_At_100_Rage = 3f;

            public static int ApplyRageDamageMul(int baseDamage, int rage)
            {
                return ApplyRageDamageMul(baseDamage, rage, Damage_Mul_At_100_Rage);
            }

            public static int ApplyRageDamageMul(int baseDamage, int rage, float mulAt100Rage)
            {
                float mul = mulAt100Rage * Math.Max(0, rage) / 100f;
                return Math.Max(1, (int)(baseDamage * mul));
            }
        }
    }
}
