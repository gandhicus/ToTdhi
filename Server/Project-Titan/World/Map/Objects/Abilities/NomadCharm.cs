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

        private readonly HashSet<Enemy> markedEnemies = new HashSet<Enemy>();

        private readonly global::World.World spawnWorld;

        private bool markingActive = true;

        private bool despawned;

        private Player ownerPlayer;

        private AbilityModifierSnapshot mods;

        public NomadCharm(float worldTime, float lifetime, Player owner, AbilityModifierSnapshot mods)
        {
            expireTime = worldTime + lifetime;
            ownerPlayer = owner;
            spawnWorld = owner?.world;
            this.mods = mods;
        }

        public NomadCharm(float worldTime) : this(worldTime, 15f, null, AbilityModifierSnapshot.Empty)
        {
        }

        public void Interact(Player player, TnInteract interact)
        {
            if (despawned) return;
            if (!healed.Add(player.GetOwnerId())) return;
            if (markingActive)
            {
                // Consume the mark on every enemy this charm reached, even after it left the radius.
                markingActive = false;
                foreach (var enemy in markedEnemies)
                    if (enemy.world == world)
                        enemy.RemoveEffect(StatusEffect.Marked);
                markedEnemies.Clear();
            }
            int heal = 100 + mods.interactHealBonus;
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
            var playerState = player.gameState.playerState;
            uint playerTime = playerState.LastClientTime;
            playerState.ApplyTimedStatBonus(StatType.Vigor, 8, playerTime, AbilityFunctions.Nomad.Interact_Vigor_Duration_Ms);

            if (ownerPlayer != null && player.GetOwnerId() == ownerPlayer.GetOwnerId())
            {
                int talismanRof = AbilityFunctions.Nomad.GetInteractTalismanRof(mods, activationRage);
                if (talismanRof > 0)
                    playerState.ApplyTimedAlternateStatBonus(AlternateStatType.RateOfFire, talismanRof,
                        playerTime, AbilityFunctions.Nomad.Interact_Talisman_RoF_Duration_Ms);
            }

            if (ownerPlayer != null)
            {
                var ownerState = ownerPlayer.gameState.playerState;
                ownerState.ability.TriggerTalisman(TalismanTrigger.Interact, ownerState.LastClientTime, position.Value, player.position.Value);
            }
        }

        protected override void DoTick(ref WorldTime time)
        {
            base.DoTick(ref time);

            if (despawned) return;

            if (time.totalTime >= expireTime)
            {
                if (ownerPlayer != null && ownerPlayer.world != null)
                {
                    var ownerState = ownerPlayer.gameState.playerState;
                    ownerState.ability.TriggerTalisman(TalismanTrigger.AbilityEnd, ownerState.LastClientTime, position.Value, position.Value);
                }
                world.objects.RemoveObjectPostLogic(this);
                return;
            }

            if (!markingActive) return;

            float radius = 1f + mods.markRadiusBonus;
            float linger = (AbilityFunctions.Nomad.Marked_Linger_Ms + System.Math.Max(0, mods.markedLingerMs)) / 1000f;
            foreach (var enemy in world.objects.GetEnemiesWithin(position.Value.x, position.Value.y, radius).ToArray())
            {
                enemy.AddEffect(StatusEffect.Marked, linger);
                markedEnemies.Add(enemy);
            }
        }

        public void Despawn()
        {
            if (despawned) return;
            despawned = true;
            markingActive = false;
            markedEnemies.Clear();
            // The manager also handles charms waiting for their throw delay to end.
            spawnWorld?.objects.RemoveObjectOrPending(this);
        }
    }
}
