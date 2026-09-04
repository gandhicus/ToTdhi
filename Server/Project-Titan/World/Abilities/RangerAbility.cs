using System;
using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data.Entities;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;
using World.Map.Objects.Entities;

namespace World.Abilities
{
    public class RangerAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Ranger;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            var rageSpent = rage;
            SpendDumpRage(ref rage, mods, out rageCost);
            SkillTreeFunctions.ApplyRageToOnUseStats(ref mods, rageSpent);
            failedToUse = false;
            sendToSelf = false;

            float maxRange = 6f + mods.abilityRangeBonus;
            var targetVec = target - position;
            var curLength = targetVec.Length;
            if (curLength > maxRange)
                target = position + targetVec.ChangeLength(maxRange, curLength);

            float rangerRadius = AbilityFunctions.Ranger.GetRadius(rageSpent, attack) + mods.abilityRadiusBonus;
            bool damaging = PlayerState.HasEffect(StatusEffect.Damaging, time);
            int weaponDamage = player.GetHeldWeaponShotDamage(time, time);
            int damage = AbilityFunctions.Ranger.ScaleWeaponDamage(weaponDamage, attack, damaging);
            damage = AbilityFunctions.RageSpend.ApplyRageDamageMul(damage, rageSpent, AbilityFunctions.Ranger.Rage_Damage_At_100);
            damage = Math.Max(1, (int)(damage * (1f + mods.abilityDamagePct)));

            Fire(time, target, rangerRadius, damage, mods, rageSpent, attack);

            if (mods.timedAttack > 0 && mods.timedAttackMs > 0)
                PlayerState.ApplyTimedStatBonus(StatType.Attack, mods.timedAttack, time, (uint)mods.timedAttackMs);

            return null;
        }

        // One rain at the aim point: hit every enemy in radius, then play the arrow VFX once.
        private void Fire(uint time, Vec2 target, float radius, int damage, AbilityModifierSnapshot mods, int rageSpent, int attack)
        {
            if (player.world == null) return;
            var hit = new List<uint>();
            var origin = player.position.Value;
            foreach (var enemy in player.world.objects.GetEnemiesWithin(target.x, target.y, radius).ToArray())
            {
                if (((EntityInfo)enemy.info).invincible || enemy.HasServerEffect(StatusEffect.Invincible) || enemy.HasServerEffect(StatusEffect.Invulnerable))
                    continue;

                hit.Add(enemy.gameId);
                var damageTaken = enemy.GetDamageTaken(damage);
                enemy.Hurt(damageTaken, player);
                player.OnDamageEnemy(enemy, damageTaken);
                TriggerTalisman(TalismanTrigger.AbilityHit, time, origin, enemy.position.Value, ref damageTaken);
                TriggerTalisman(TalismanTrigger.AbilityTick, time, origin, target);

                if (mods.slowMs > 0)
                    enemy.AddEffect(StatusEffect.Slowed, mods.slowMs / 1000f);

                if (enemy.GetHealth() <= 0)
                    enemy.Die(player);
                if (mods.rageOnKill > 0 && enemy.IsDead)
                    PlayerState.AddRage(time, mods.rageOnKill, false);
                if (hit.Count == 255) break;
            }

            var rangerFx = new RangerAbilityWorldEffect(hit.ToArray(), target, (byte)rageSpent, attack, radius, damage);
            ColorWorldEffect(rangerFx);
            var packet = new TnPlayEffect(rangerFx);
            if (player.client != null)
                player.client.SendAsync(packet);
            foreach (var other in player.playersSentTo)
            {
                if (other == player || other?.client == null) continue;
                other.client.SendAsync(packet);
            }

            TriggerTalisman(TalismanTrigger.AbilityEnd, time, origin, target);
        }
    }
}
