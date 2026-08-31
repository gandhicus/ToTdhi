using System;
using System.Collections.Generic;
using System.Text;
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
                    return 80;
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
                    return 2000;
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

            public static int GetGroundDurationMs(byte rage)
            {
                return 1000 + (int)((rage / 100.0f) * 10000);
            }

            public static float GetRadius(byte rage)
            {
                return 6;
            }
        }

        public static class Lancer
        {
            private const float Angle_Offset = 5f * (AngleUtils.PI / 180.0f);

            public const int Rage_Cost = 5;

            public static float GetAngleOffset(uint projId)
            {
                var offsetId = projId % 5;
                switch (offsetId)
                {
                    case 0:
                        return Angle_Offset;
                    case 1:
                        return Angle_Offset * -2;
                    case 2:
                        return 0;
                    case 3:
                        return Angle_Offset * -1;
                    case 4:
                        return Angle_Offset * 2;
                }
                return 0;
            }

            public static float GetProjectileSize(int rage)
            {
                return 1f + (rage / 100f);
            }

            public static int GetProjectileDamage(int rage, int attack)
            {
                var damage = 10 + rage;
                return (int)(damage * (0.5f + attack / 50f));
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
            public static float GetRadius(int rage, int attack)
            {
                return 2 + (rage / 100f) * 4;
            }

            public static ushort GetDamage(int rage, int attack)
            {
                var attackScalar = 0.5f + attack / 75f;
                var rageScalar = rage / 100f;
                return (ushort)(10 + (80 + attackScalar * 1100) * rageScalar);
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

            public static int GetProjectileDamage(int rage, int attack)
            {
                var damage = 10 + rage * 45;
                return (int)(damage * (0.5f + attack / 75f));
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

            public const int RoF_Amount = 5;

            public const uint RoF_Duration_Ms = 4000;
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
        }
    }
}
