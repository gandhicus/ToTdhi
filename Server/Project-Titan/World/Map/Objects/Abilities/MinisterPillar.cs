using System;
using System.Collections.Generic;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Net.Packets.Server;
using Utils.NET.Collections;
using Utils.NET.Geometry;
using World.Map.Objects.Entities;

namespace World.Map.Objects.Abilities
{
    public class MinisterPillar : GameObject
    {
        public override GameObjectType Type => GameObjectType.StaticObject;

        public override bool Ticks => true;

        private float radius;
        private float startTime;
        private float endTime;
        private int healAmount;
        private float tickSec;
        private AbilityModifierSnapshot mods;
        private Player owner;
        private ExpirationQueue<uint> healedExpiration;
        private HashSet<uint> healed = new HashSet<uint>();
        private HashSet<uint> absorptionGiven = new HashSet<uint>();

        public MinisterPillar(int rage, int attack, Vec2 position, float time)
            : this(null, AbilityFunctions.Minister.GetHealAmount(rage, attack), AbilityFunctions.Minister.GetPillarRadius(rage), AbilityFunctions.Minister.GetPillarDurationMs(rage) / 1000f, 2f, AbilityModifierSnapshot.Empty, time)
        {
            this.position.Value = position;
        }

        public MinisterPillar(Player owner, int healAmount, float radius, float durationSec, float tickSec, AbilityModifierSnapshot mods, float time)
        {
            this.owner = owner;
            this.healAmount = healAmount;
            this.radius = radius;
            this.tickSec = tickSec;
            this.mods = mods;
            startTime = time + 0.5f;
            endTime = startTime + durationSec;
            healedExpiration = new ExpirationQueue<uint>(tickSec);
        }

        protected override void DoTick(ref WorldTime time)
        {
            base.DoTick(ref time);

            if (time.totalTime < startTime) return;

            if (time.totalTime >= endTime)
            {
                ClearFieldBonuses();
                if (owner != null && owner.world != null)
                {
                    var now = owner.gameState.playerState.LastClientTime;
                    owner.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.AbilityEnd, now, position.Value, position.Value);
                }
                world.objects.RemoveObjectPostLogic(this);
                return;
            }

            foreach (var p in healedExpiration.GetExpired())
                healed.Remove(p);

            foreach (var player in world.objects.GetPlayersWithin(position.Value.x, position.Value.y, radius))
            {
                Heal(player);
                var now = player.gameState.playerState.LastClientTime;
                int vigor = 8 + mods.vigorBonus;
                var state = player.gameState.playerState;
                uint vigorMs = mods.vigorBonusMs > 0 ? (uint)mods.vigorBonusMs : 1200;
                state.ApplyMinisterFieldStatBonus(StatType.Vigor, vigor, now, vigorMs);
                if (mods.timedAttack > 0)
                    state.ApplyMinisterFieldStatBonus(StatType.Attack, mods.timedAttack, now, (uint)mods.timedAttackMs);
                if (mods.fieldDefense > 0)
                    state.ApplyMinisterFieldStatBonus(StatType.Defense, mods.fieldDefense, now, (uint)mods.fieldDefenseMs);
                // Once per player: re-applying every tick extends the 6s timer for the whole pillar lifetime.
                if (mods.absorptionChance > 0 && absorptionGiven.Add(player.gameId))
                    state.ApplyTimedAlternateStatBonus(AlternateStatType.AbsorptionChance, mods.absorptionChance, now, (uint)mods.absorptionChanceMs);
            }
        }

        private void ClearFieldBonuses()
        {
            foreach (var player in world.objects.GetPlayersWithin(position.Value.x, position.Value.y, radius))
                player.gameState.playerState.ClearMinisterFieldBonuses();
        }

        private void Heal(Player player)
        {
            if (!healed.Add(player.gameId)) return;
            healedExpiration.Enqueue(player.gameId);
            player.Heal(healAmount);
            if (owner != null)
            {
                var now = owner.gameState.playerState.LastClientTime;
                owner.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.AbilityTick, now, position.Value, player.position.Value);
            }

            var pkt = new TnPlayEffect(new HealLaserWorldEffect(gameId, player.gameId));
            foreach (var p in player.playersSentTo)
                p.client.SendAsync(pkt);
        }
    }
}
