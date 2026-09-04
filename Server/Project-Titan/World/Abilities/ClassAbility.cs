using System;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data.Components;
using TitanCore.Data.Entities;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using Utils.NET.Utils;
using World.GameState;
using World.Map.Objects.Entities;

namespace World.Abilities
{
    public abstract class ClassAbility
    {
        private static TypeFactory<ClassType, ClassAbility> classAbilityFactory = new TypeFactory<ClassType, ClassAbility>(_ => _.ClassType);

        public static ClassAbility GetAbility(ClassType classType)
        {
            return classAbilityFactory.Create(classType);
        }

        public abstract ClassType ClassType { get; }

        protected Player player;

        protected PlayerState PlayerState => player.gameState.playerState;

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        public abstract void OnHit(EntityState entity, uint time, ref int damageTaken);

        public virtual void OnHit(EntityState entity, uint time, ref int damageTaken, ushort sourceItem)
        {
            OnHit(entity, time, ref damageTaken);
        }

        public virtual void OnHit(EntityState entity, uint time, ref int damageTaken, ushort sourceItem, uint projectileStartTime)
        {
            OnHit(entity, time, ref damageTaken, sourceItem);
        }

        public abstract TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse);

        public abstract void OnMove(Vec2 position, uint time);

        public virtual void Tick(uint time)
        {
        }

        public virtual void WorldTick(ref WorldTime time)
        {
        }

        protected TnPlayEffect PlayColored(WorldEffect effect)
        {
            ColorWorldEffect(effect);
            return new TnPlayEffect(effect);
        }

        protected void ColorWorldEffect(WorldEffect effect)
        {
            if (!SkillTreeFunctions.IsEnabled) return;
            TalismanEffect.ApplyAbilityAoeColor(effect, PlayerState.abilityMods.talismanEffects, PlayerState.abilityActivationRage);
        }

        protected void SpendDumpRage(ref byte rage, AbilityModifierSnapshot mods, out byte rageCost)
        {
            AbilityFunctions.RageSpend.SpendDumpRage(ref rage, mods, out rageCost);
        }

        public void TriggerTalisman(TalismanTrigger trigger, uint time, Vec2 position, Vec2 target, float? abilityRagePercent = null)
        {
            int damageTaken = 0;
            TriggerTalisman(trigger, time, position, target, ref damageTaken, null, abilityRagePercent, 0);
        }

        public void TriggerTalisman(TalismanTrigger trigger, uint time, Vec2 position, Vec2 target, ref int damageTaken, float? abilityRagePercent = null)
        {
            TriggerTalisman(trigger, time, position, target, ref damageTaken, null, abilityRagePercent, 0);
        }

        public void TriggerTalisman(TalismanTrigger trigger, uint time, Vec2 position, Vec2 target, ref int damageTaken, Entity hitEnemy, float? abilityRagePercent = null, uint projectileStartTime = 0, uint shotTargetId = 0)
        {
            if (!SkillTreeFunctions.IsEnabled) return;
            var effects = PlayerState.abilityMods.talismanEffects;
            if (effects == null || effects.Length == 0) return;

            var ragePercent = abilityRagePercent ?? PlayerState.abilityActivationRage;

            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                if (effect.trigger != trigger) continue;
                if (!TalismanEffect.MeetsRageThreshold(ragePercent, effect)) continue;
                if (Math.Abs(effect.damageMul - 1f) > 0.001f && damageTaken > 0)
                    damageTaken = (int)(damageTaken * effect.damageMul);
                if (!PlayerState.TryConsumeTalismanCooldown(i, effect.cooldownMs, time)) continue;

                if (effect.statBonus != null)
                    PlayerState.ApplyTalismanTimedStatBonus(effect.statBonus.statType, effect.statBonus.amount, time, effect.statBonus.durationMs, i);

                if (effect.alternateStatBonus != null)
                    PlayerState.ApplyTimedAlternateStatBonus(effect.alternateStatBonus.statType, effect.alternateStatBonus.amount, time, effect.alternateStatBonus.durationMs);

                if (effect.healAmount > 0)
                    player.Heal(effect.healAmount);

                if (effect.rageGain > 0)
                {
                    uint targetId = shotTargetId != 0 ? shotTargetId : (hitEnemy != null ? hitEnemy.gameId : 0);
                    if (projectileStartTime == 0 || PlayerState.TryConsumeTalismanShotRage(i, targetId, projectileStartTime))
                        PlayerState.AddRage(time, effect.rageGain, false);
                }

                if (hitEnemy != null && effect.statusEffects != null)
                {
                    for (int e = 0; e < effect.statusEffects.Length; e++)
                    {
                        var hit = effect.statusEffects[e];
                        if (hit.duration <= 0) continue;
                        if (hitEnemy.world == null || hitEnemy.IsDead) break;
                        hitEnemy.AddEffect(hit.type, hit.duration / 1000f, hit.amount);
                    }
                }

                if (effect.aoe != null)
                    FireAoe(effect.aoe, time, position, target, effect.hasAoeColor, effect.aoeColor);
            }
        }

        public void FireAoe(TalismanAoe aoe, uint time, Vec2 position, Vec2 target)
        {
            FireAoe(aoe, time, position, target, false, default);
        }

        public void FireAoe(TalismanAoe aoe, uint time, Vec2 position, Vec2 target, bool hasFallbackColor, GameColor fallbackColor)
        {
            if (aoe == null) return;
            Vec2 blast;
            if (aoe.at == TalismanAoeAt.Self)
            {
                blast = position;
            }
            else if (aoe.at == TalismanAoeAt.RandomTarget)
            {
                var searchRadius = aoe.range > 0 ? aoe.range : 10f;
                var randomEnemy = player.world.objects.GetRandomEnemy(position, searchRadius);
                if (randomEnemy == null) return;
                blast = randomEnemy.position.Value;
            }
            else
            {
                blast = target;
                if (aoe.range > 0)
                {
                    var delta = blast - position;
                    var length = delta.Length;
                    if (length > aoe.range)
                        blast = position + delta.ChangeLength(aoe.range, length);
                }
            }

            foreach (var enemy in player.world.objects.GetEnemiesWithin(blast.x, blast.y, aoe.radius).ToArray())
            {
                if (enemy.world == null || enemy.IsDead) continue;
                if (((EntityInfo)enemy.info).invincible || enemy.HasServerEffect(StatusEffect.Invincible) || enemy.HasServerEffect(StatusEffect.Invulnerable))
                    continue;

                if (aoe.statusEffects != null)
                {
                    for (int i = 0; i < aoe.statusEffects.Length; i++)
                    {
                        var hit = aoe.statusEffects[i];
                        if (hit.duration <= 0) continue;
                        if (enemy.world == null) break;
                        enemy.AddEffect(hit.type, hit.duration / 1000f, hit.amount);
                    }
                }

                if (aoe.damage > 0 && enemy.world != null && !enemy.IsDead)
                {
                    var damageTaken = aoe.trueDamage ? aoe.damage : enemy.GetDamageTaken(aoe.damage);
                    enemy.Hurt(damageTaken, player);
                    enemy.ServerDamage(damageTaken, player.info);
                    player.OnDamageEnemy(enemy, damageTaken);
                    if (enemy.GetHealth() <= 0)
                        enemy.Die(player);
                }
            }

            var blastEffect = new BombBlastWorldEffect(position, blast, aoe.radius, Math.Max(0.05f, aoe.lifetime));
            if (aoe.hasColor)
            {
                blastEffect.hasColor = true;
                blastEffect.color = aoe.color;
            }
            else if (hasFallbackColor)
            {
                blastEffect.hasColor = true;
                blastEffect.color = fallbackColor;
            }
            var packet = new TnPlayEffect(blastEffect);
            player.client.SendAsync(packet);
            foreach (var other in player.playersSentTo)
            {
                if (other != player)
                    other.client.SendAsync(packet);
            }
        }
    }
}
