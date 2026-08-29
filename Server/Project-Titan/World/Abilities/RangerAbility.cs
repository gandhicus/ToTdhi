using System;
using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data.Entities;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;
using World.Map.Objects.Entities;
using World.Net;

namespace World.Abilities
{
    public class RangerAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Ranger;

        private class RainVolley
        {
            public Vec2 target;
            public float radius;
            public int tickDamage;
            public AbilityModifierSnapshot mods;
            public int ticksLeft;
            public uint nextTime;
            public uint step;
            public uint lastFireTime;
        }

        private readonly List<RainVolley> rains = new List<RainVolley>();

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override void Tick(uint time)
        {
            for (int i = rains.Count - 1; i >= 0; i--)
            {
                var rain = rains[i];
                // Skip if this client-time already spawned a volley (UseAbility fires
                // immediately, then Tick can run in the same packet burst).
                if (time < rain.nextTime || time <= rain.lastFireTime) continue;
                bool last = rain.ticksLeft <= 1;
                Fire(time, rain, last);
                rain.ticksLeft--;
                if (last)
                {
                    rains.RemoveAt(i);
                    continue;
                }
                rain.nextTime = rain.nextTime + rain.step;
            }
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            var rageSpent = rage;
            SpendDumpRage(ref rage, mods, out rageCost);
            failedToUse = false;
            sendToSelf = false;

            float maxRange = 6f + mods.abilityRangeBonus;
            var targetVec = target - position;
            var curLength = targetVec.Length;
            if (curLength > maxRange)
                target = position + targetVec.ChangeLength(maxRange, curLength);

            float rangerRadius = AbilityFunctions.Ranger.GetRadius(rageSpent, attack) + mods.abilityRadiusBonus;
            int totalDamage = AbilityFunctions.Ranger.GetDamage(rageSpent, attack);
            totalDamage = (int)(totalDamage * (1f + mods.abilityDamagePct));
            int tickDamage = Math.Max(1, totalDamage / 3);
            int extraTicks = 2;
            if (mods.durationBonusMs >= 240)
                extraTicks = 3;

            uint step = 300 + (uint)Math.Max(0, mods.durationBonusMs) / 3;
            if (step < 1)
                step = 1;

            var rain = new RainVolley
            {
                target = target,
                radius = rangerRadius,
                tickDamage = tickDamage,
                mods = mods,
                ticksLeft = extraTicks,
                step = step,
                nextTime = time + step,
                lastFireTime = 0
            };
            Fire(time, rain, extraTicks <= 0);
            if (extraTicks > 0)
                rains.Add(rain);
            return null;
        }

        private void Fire(uint time, RainVolley rain, bool last)
        {
            if (player.world == null) return;
            var hit = new List<uint>();
            var origin = player.position.Value;
            var target = rain.target;
            foreach (var enemy in player.world.objects.GetEnemiesWithin(target.x, target.y, rain.radius).ToArray())
            {
                if (((EntityInfo)enemy.info).invincible || enemy.HasServerEffect(StatusEffect.Invincible) || enemy.HasServerEffect(StatusEffect.Invulnerable))
                    continue;

                hit.Add(enemy.gameId);
                var damageTaken = enemy.GetDamageTaken(rain.tickDamage);
                enemy.Hurt(damageTaken, player);
                player.OnDamageEnemy(enemy, damageTaken);
                TriggerTalisman(TalismanTrigger.AbilityHit, time, origin, enemy.position.Value, ref damageTaken);
                TriggerTalisman(TalismanTrigger.AbilityTick, time, origin, target);

                if (rain.mods.slowMs > 0)
                    enemy.AddEffect(StatusEffect.Slowed, rain.mods.slowMs / 1000f);

                if (enemy.GetHealth() <= 0)
                    enemy.Die(player);
                if (rain.mods.rageOnKill > 0 && enemy.IsDead)
                    PlayerState.AddRage(time, rain.mods.rageOnKill, false);
                if (hit.Count == 255) break;
            }

            rain.lastFireTime = time;

            var rangerFx = new RangerAbilityWorldEffect(hit.ToArray(), target, 50, 50);
            ColorWorldEffect(rangerFx);
            var packet = new TnPlayEffect(rangerFx);
            // One packet per connection — caster is often also in playersSentTo.
            var sent = new HashSet<Client>();
            if (player.client != null && sent.Add(player.client))
                player.client.SendAsync(packet);
            foreach (var other in player.playersSentTo)
            {
                if (other?.client == null) continue;
                if (!sent.Add(other.client)) continue;
                other.client.SendAsync(packet);
            }

            if (last)
                TriggerTalisman(TalismanTrigger.AbilityEnd, time, origin, target);
        }
    }
}
