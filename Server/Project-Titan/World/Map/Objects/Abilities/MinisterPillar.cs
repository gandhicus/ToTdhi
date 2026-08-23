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
                if (owner != null && owner.world != null)
                    owner.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.AbilityEnd, (uint)(time.totalTime * 1000), position.Value, position.Value);
                world.objects.RemoveObjectPostLogic(this);
                return;
            }

            foreach (var p in healedExpiration.GetExpired())
                healed.Remove(p);

            uint now = (uint)(time.totalTime * 1000);
            foreach (var player in world.objects.GetPlayersWithin(position.Value.x, position.Value.y, radius))
            {
                Heal(player);
                int vigor = 8 + mods.vigorBonus;
                player.gameState.playerState.ApplyTimedStatBonus(StatType.Vigor, vigor, now, 1200);
                if (mods.timedAttack > 0)
                    player.gameState.playerState.ApplyTimedStatBonus(StatType.Attack, mods.timedAttack, now, 1200);
                if (mods.fieldDefense > 0)
                    player.gameState.playerState.ApplyTimedStatBonus(StatType.Defense, mods.fieldDefense, now, 1200);
            }
        }

        private void Heal(Player player)
        {
            if (!healed.Add(player.gameId)) return;
            healedExpiration.Enqueue(player.gameId);
            player.Heal(healAmount);
            if (owner != null)
                owner.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.AbilityTick, (uint)(world.time.totalTime * 1000), position.Value, player.position.Value);

            var pkt = new TnPlayEffect(new HealLaserWorldEffect(gameId, player.gameId));
            foreach (var p in player.playersSentTo)
                p.client.SendAsync(pkt);
        }
    }
}
