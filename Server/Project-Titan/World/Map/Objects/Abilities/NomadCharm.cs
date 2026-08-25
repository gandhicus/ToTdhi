using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Components;
using TitanCore.Net.Packets.Client;
using World.Map.Objects.Entities;
using World.Map.Objects.Interfaces;

namespace World.Map.Objects.Abilities
{
    public class NomadCharm : GameObject, IInteractable
    {
        public override GameObjectType Type => GameObjectType.NomadCharm;

        public override bool Ticks => true;

        private float expireTime;

        private HashSet<ulong> healed = new HashSet<ulong>();

        private Player ownerPlayer;

        private AbilityModifierSnapshot mods;

        public NomadCharm(float worldTime, float lifetime, Player owner, AbilityModifierSnapshot mods)
        {
            expireTime = worldTime + lifetime;
            ownerPlayer = owner;
            this.mods = mods;
        }

        public NomadCharm(float worldTime) : this(worldTime, 15f, null, AbilityModifierSnapshot.Empty)
        {
        }

        public void Interact(Player player, TnInteract interact)
        {
            if (!healed.Add(player.GetOwnerId())) return;
            int heal = 120 + mods.interactHealBonus;
            var activationRage = ownerPlayer?.gameState.playerState.abilityActivationRage ?? 0;
            if (SkillTreeFunctions.IsEnabled && mods.talismanEffects != null)
            {
                for (int i = 0; i < mods.talismanEffects.Length; i++)
                {
                    var effect = mods.talismanEffects[i];
                    if (effect.trigger == TalismanTrigger.Interact && effect.healAmount < 0
                        && TalismanEffect.MeetsRageThreshold(activationRage, effect))
                        heal += effect.healAmount;
                }
            }
            if (heal > 0)
                player.Heal(heal);
            player.gameState.playerState.ApplyTimedStatBonus(StatType.Vigor, 8, (uint)(world.time.totalTime * 1000), 6000);

            if (ownerPlayer != null && player.GetOwnerId() == ownerPlayer.GetOwnerId())
            {
                uint rofMs = AbilityFunctions.Nomad.RoF_Duration_Ms + (uint)System.Math.Max(0, mods.rofDurationBonusMs);
                int rofAmt = AbilityFunctions.Nomad.RoF_Amount;
                if (SkillTreeFunctions.IsEnabled && mods.talismanEffects != null)
                {
                    for (int i = 0; i < mods.talismanEffects.Length; i++)
                    {
                        var effect = mods.talismanEffects[i];
                        if (effect.trigger == TalismanTrigger.Interact
                            && TalismanEffect.MeetsRageThreshold(activationRage, effect))
                            rofAmt += effect.rofAmount;
                    }
                }
                player.gameState.playerState.ApplyTimedAlternateStatBonus(AlternateStatType.RateOfFire, rofAmt, (uint)(world.time.totalTime * 1000), rofMs);
            }

            if (ownerPlayer != null)
                ownerPlayer.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.Interact, (uint)(world.time.totalTime * 1000), position.Value, player.position.Value);
        }

        protected override void DoTick(ref WorldTime time)
        {
            base.DoTick(ref time);

            if (time.totalTime >= expireTime)
            {
                if (ownerPlayer != null && ownerPlayer.world != null)
                    ownerPlayer.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.AbilityEnd, (uint)(time.totalTime * 1000), position.Value, position.Value);
                world.objects.RemoveObjectPostLogic(this);
                return;
            }

            float radius = 1f + mods.markRadiusBonus;
            float linger = (AbilityFunctions.Nomad.Marked_Linger_Ms + System.Math.Max(0, mods.markedLingerMs)) / 1000f;
            foreach (var enemy in world.objects.GetEnemiesWithin(position.Value.x, position.Value.y, radius).ToArray())
                enemy.AddEffect(StatusEffect.Marked, linger);
        }
    }
}
