using System;
using TitanCore.Core;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Models;
using TitanCore.Net.Packets.Server;
using TitanDatabase.Models;
using World.GameState;

namespace World.Map.Objects.Entities
{
    public partial class Player
    {
        public void RebuildAbilityModifiers()
        {
            if (gameState?.playerState == null) return;
            gameState.playerState.abilityMods = BuildAbilityModifiers();
        }

        public AbilityModifierSnapshot BuildAbilityModifiers()
        {
            if (!SkillTreeFunctions.IsUnlocked(GetLevel()))
                return AbilityModifierSnapshot.Empty;

            var equips = new Item[4];
            for (int i = 0; i < 4; i++)
                equips[i] = GetItem(i)?.itemData ?? Item.Blank;

            var talisman = character?.talismanItem?.itemData ?? Item.Blank;
            return SkillTreeFunctions.BuildSnapshot((ClassType)info.id, character?.talentRanks ?? 0, equips, talisman);
        }

        public void SendSkillTreeState()
        {
            if (!SkillTreeFunctions.IsEnabled || client == null) return;
            var talisman = character?.talismanItem?.itemData ?? Item.Blank;
            client.SendAsync(new TnSkillTreeState(character?.talentRanks ?? 0, talisman));
        }

        public bool TryUnlockTalent(SkillTreeNode node, out string error)
        {
            error = null;
            if (!SkillTreeFunctions.IsUnlocked(GetLevel()))
            {
                error = $"Skill tree unlocks at level {SkillTreeFunctions.Unlock_Level}.";
                return false;
            }

            var packed = character.talentRanks;
            int spent = SkillTreeFunctions.GetSpentRank(packed, node);
            if (spent >= SkillTreeFunctions.Max_Spent_Rank)
            {
                error = "That node is already maxed.";
                return false;
            }
            if (SkillTreeFunctions.GetSpentTotal(packed) >= SkillTreeFunctions.Point_Cap)
            {
                error = "No skill points remaining.";
                return false;
            }

            int cost = SkillTreeFunctions.GetRankCost(spent + 1);
            if (fullSouls.Value < cost)
            {
                error = "Not enough essence.";
                return false;
            }

            TakeEssence(cost);
            character.talentRanks = SkillTreeFunctions.SetSpentRank(packed, node, spent + 1);
            RebuildAbilityModifiers();
            SendSkillTreeState();
            return true;
        }

        public ChatData RespecSkills()
        {
            if (!SkillTreeFunctions.IsEnabled)
                return ChatData.Error("Skill tree is disabled.");

            var packed = character?.talentRanks ?? 0;
            if (SkillTreeFunctions.GetSpentTotal(packed) == 0)
                return ChatData.Error("Your skills are already at their base values.");

            character.talentRanks = 0;
            RebuildAbilityModifiers();
            SendSkillTreeState();
            return ChatData.Info("Your skills have been reset. Essence spent on the skill tree was not refunded.");
        }

        public bool CanSocketTalisman(ServerItem item, out string error)
        {
            error = null;
            if (item == null)
                return true;
            if (!(item.itemData.GetInfo() is EquipmentInfo equip) || equip.slotType != SlotType.Talisman)
            {
                error = "That is not a talisman.";
                return false;
            }
            if (equip.requiredClass != 0 && (ushort)equip.requiredClass != info.id)
            {
                error = $"That talisman is for {equip.requiredClass}.";
                return false;
            }
            return true;
        }
    }
}
