using System;
using System.Collections.Generic;
using TitanCore.Data.Components;
using TitanCore.Data.Items;
using TitanCore.Net;

namespace TitanCore.Core
{
    public enum SkillTreeNode : byte
    {
        Slot0 = 0,
        Slot1 = 1,
        Slot2 = 2,
        Slot3 = 3,
        Slot4 = 4,
        Slot5 = 5,
        Slot6 = 6,
        Slot7 = 7
    }

    public struct AbilityModifierSnapshot
    {
        public static AbilityModifierSnapshot Empty => default;

        public float healPower;
        public float weaponDamagePct;
        public float cooldownMul;
        public int cooldownFlatMs;
        public int durationBonusMs;
        public float durationMul;
        public int pulseLockoutMs;
        public float rageKeep;
        public float rageCostFlat;
        public int hymnDefense;
        public int hymnDefenseMs;
        public int hymnMaxHealth;
        public int hymnMaxHealthMs;
        public int hymnMaxHealthRageChunk;
        public int hymnBlockChance;
        public int hymnBlockChanceMs;
        public float abilityDamagePct;
        public float abilityRadiusBonus;
        public float abilityRangeBonus;
        public int slowMs;
        public int rageOnKill;
        public float projectileSizePct;
        public float wobbleMul;
        public int pierce;
        public int timedAttack;
        public int timedAttackMs;
        public int timedDefenseMs;
        public int speedOnHit;
        public int speedOnHitMs;
        public int postDashInvulnMs;
        public float markedDamagePct;
        public int markedRage;
        public int interactHealBonus;
        public float markRadiusBonus;
        public int rofDurationBonusMs;
        public int markedLingerMs;
        public int rofAmount;
        public int vigorBonus;
        public int vigorBonusMs;
        public int absorptionChance;
        public int absorptionChanceMs;
        public int absorptionRageChunk;
        public float selfHealBurstPct;
        public int fieldDefense;
        public int fieldDefenseMs;
        public float shoutSpreadDeg;
        public TalismanEffect[] talismanEffects;
    }


    public static class SkillTreeFunctions
    {
        public const int Node_Count = 8;
        public const int Max_Spent_Rank = 3;
        public const int Max_Effective_Rank = 6;
        public const int Point_Cap = 9;
        public const int Talisman_Slot = 12;

        /// Character level required to see and use the tree
        public const int Unlock_Level = NetConstants.Class_Quest_Level_2;

        public static readonly int[] Rank_Essence_Cost = { 0, 150, 300, 450 };

        public static ClassSkillTrees.NodeDef GetNode(ClassType classType, SkillTreeNode node)
        {
            var nodes = ClassSkillTrees.GetNodes(classType);
            int i = (int)node;
            if (i < 0 || i >= nodes.Length)
                return nodes[0];
            return nodes[i];
        }

        public static string GetNodeName(ClassType classType, SkillTreeNode node) => GetNode(classType, node).name;

        public static string GetNodeSprite(ClassType classType, SkillTreeNode node) => GetNode(classType, node).sprite;

        public static EffectStyle GetNodeStyle(ClassType classType, SkillTreeNode node) => GetNode(classType, node).style;

        public const int Base_Pulse_Lockout_Ms = 1000;
        public const uint Base_Hymn_Duration_Ms = 8000;

        public const int On_Use_Stat_Rage_Chunk = 25;

        public static bool IsEnabled => NetConstants.Use_Skill_Tree;

        public static bool IsUnlocked(int level) => IsEnabled && level >= Unlock_Level;

        public static int GetSpentRank(uint packed, SkillTreeNode node)
        {
            return (int)((packed >> ((int)node * 2)) & 0x3);
        }

        public static uint SetSpentRank(uint packed, SkillTreeNode node, int rank)
        {
            rank = Math.Max(0, Math.Min(Max_Spent_Rank, rank));
            int shift = (int)node * 2;
            packed &= ~(0x3u << shift);
            packed |= (uint)rank << shift;
            return packed;
        }

        public static int GetSpentTotal(uint packed)
        {
            int total = 0;
            for (int i = 0; i < Node_Count; i++)
                total += GetSpentRank(packed, (SkillTreeNode)i);
            return total;
        }

        public static int GetSpentEssence(uint packed)
        {
            int total = 0;
            for (int i = 0; i < Node_Count; i++)
            {
                int spent = GetSpentRank(packed, (SkillTreeNode)i);
                for (int rank = 1; rank <= spent; rank++)
                    total += GetRankCost(rank);
            }
            return total;
        }

        public static int GetRankCost(int nextRank)
        {
            if (nextRank < 1 || nextRank >= Rank_Essence_Cost.Length)
                return int.MaxValue;
            return Rank_Essence_Cost[nextRank];
        }

        public static int ClampEffective(int spent, int gear)
        {
            return Math.Min(Max_Effective_Rank, Math.Max(0, spent) + Math.Max(0, gear));
        }

        public static float Scale(float perRank, int rank)
        {
            return perRank * Math.Max(0, rank);
        }

        public static int Scale(int perRank, int rank)
        {
            return perRank * Math.Max(0, rank);
        }

        public static int ScaleOnUseRank(int perRank, int rank)
        {
            rank = Math.Max(0, rank);
            if (rank <= 2)
                return perRank * rank;
            return perRank * 2 + (rank - 2);
        }

        public static int ScaleOnUseStat(int perChunkAtRank, int rageSpent)
        {
            return ScaleOnUseStat(perChunkAtRank, rageSpent, On_Use_Stat_Rage_Chunk);
        }

        public static int ScaleOnUseStat(int perChunkAtRank, int rageSpent, int rageChunk)
        {
            if (perChunkAtRank <= 0 || rageSpent <= 0 || rageChunk < 1)
                return 0;
            return perChunkAtRank * (rageSpent / rageChunk);
        }

        public static void ApplyRageToOnUseStats(ref AbilityModifierSnapshot mods, int rageSpent)
        {
            mods.hymnDefense = ScaleOnUseStat(mods.hymnDefense, rageSpent);
            int maxHealthChunk = mods.hymnMaxHealthRageChunk > 0 ? mods.hymnMaxHealthRageChunk : On_Use_Stat_Rage_Chunk;
            mods.hymnMaxHealth = ScaleOnUseStat(mods.hymnMaxHealth, rageSpent, maxHealthChunk);
            mods.hymnBlockChance = ScaleOnUseStat(mods.hymnBlockChance, rageSpent);
            mods.timedAttack = ScaleOnUseStat(mods.timedAttack, rageSpent);
            mods.speedOnHit = ScaleOnUseStat(mods.speedOnHit, rageSpent);
            mods.vigorBonus = ScaleOnUseStat(mods.vigorBonus, rageSpent);
            mods.fieldDefense = ScaleOnUseStat(mods.fieldDefense, rageSpent);
            mods.rofAmount = ScaleOnUseStat(mods.rofAmount, rageSpent);
            int absorptionChunk = mods.absorptionRageChunk > 0 ? mods.absorptionRageChunk : On_Use_Stat_Rage_Chunk;
            mods.absorptionChance = ScaleOnUseStat(mods.absorptionChance, rageSpent, absorptionChunk);
        }

        public static AbilityModifierSnapshot BuildSnapshot(ClassType classType, uint packedRanks, Item[] equips, Item talisman)
        {
            var snap = AbilityModifierSnapshot.Empty;
            if (!IsEnabled) return snap;

            int[] gear = new int[Node_Count];
            if (equips != null)
            {
                for (int i = 0; i < equips.Length; i++)
                    AddGearRanks(equips[i], classType, gear);
            }

            var ranks = new int[Node_Count];
            for (int i = 0; i < Node_Count; i++)
                ranks[i] = ClampEffective(GetSpentRank(packedRanks, (SkillTreeNode)i), gear[i]);

            snap.cooldownMul = 1f;
            snap.wobbleMul = 1f;
            snap.durationMul = 1f;
            ClassSkillTrees.ApplyRanks(classType, ranks, ref snap);

            if (!talisman.IsBlank && talisman.GetInfo() is EquipmentInfo equip && equip.talismanEffects != null && equip.talismanEffects.Count > 0)
            {
                snap.talismanEffects = equip.talismanEffects.ToArray();
                for (int i = 0; i < snap.talismanEffects.Length; i++)
                {
                    float healMul = snap.talismanEffects[i].healMul;
                    if (healMul > 0f && Math.Abs(healMul - 1f) > 0.001f)
                        snap.healPower = (snap.healPower + 1f) * healMul - 1f;
                }
            }

            return snap;
        }

        public static void AddGearRanks(Item item, ClassType classType, int[] gear)
        {
            if (item.IsBlank) return;
            if (!(item.GetInfo() is EquipmentInfo equip) || equip.talentRanks == null) return;
            foreach (var bonus in equip.talentRanks)
            {
                if (bonus.amount == 0) continue;
                if (bonus.classType != 0 && bonus.classType != classType) continue;
                if (!TryParseNode(classType, bonus.node, out var node)) continue;
                gear[(int)node] += bonus.amount;
            }
        }

        public static bool TryParseNode(ClassType classType, string name, out SkillTreeNode node)
        {
            node = SkillTreeNode.Slot0;
            if (string.IsNullOrEmpty(name))
                return false;
            if (int.TryParse(name, out int index) && index >= 1 && index <= Node_Count)
            {
                node = (SkillTreeNode)(index - 1);
                return true;
            }
            var nodes = ClassSkillTrees.GetNodes(classType);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (string.Equals(nodes[i].name, name, StringComparison.OrdinalIgnoreCase))
                {
                    node = (SkillTreeNode)i;
                    return true;
                }
            }
            return false;
        }

        public static string DescribeTalismanEffects(IList<TalismanEffect> effects)
        {
            if (effects == null || effects.Count == 0)
                return "";
            if (effects.Count == 1)
                return effects[0].Describe();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < effects.Count; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                sb.Append(effects[i].Describe());
            }
            return sb.ToString();
        }

        public static string GetNodeEffect(ClassType classType, SkillTreeNode node, int effective)
        {
            return GetNode(classType, node).effect(effective);
        }

        public static string DescribeNode(ClassType classType, SkillTreeNode node, int spent, int gear, int essence, int pointsLeft)
        {
            int nowRank = ClampEffective(spent, gear);
            int nextRank = nowRank + 1;
            var name = GetNodeName(classType, node);
            string effect = GetNodeEffect(classType, node, nowRank);
            var gearLine = gear > 0 ? $"  (gear +{gear})" : "";
            string nextLine = $"\nNext: {GetNodeEffect(classType, node, nextRank)}";
            var style = GetNodeStyle(classType, node);
            return $"{name}\nRank {spent}/{Max_Spent_Rank}{gearLine}\nNow: {effect}{nextLine}\n{style}";
        }
    }
}
