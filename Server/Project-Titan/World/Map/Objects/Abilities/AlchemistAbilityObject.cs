using System;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data;
using World.Map.Objects.Entities;

namespace World.Map.Objects.Abilities
{
    public class AlchemistAbilityObject : GameObject
    {
        public override GameObjectType Type => GameObjectType.GroundObject;

        public override bool Ticks => true;

        private AlchemistAbilityWorldEffect effect;
        private float radius;
        private float startTime;
        private float nextTick;
        private float endTime;
        private Player owner;
        private int damage;
        private AbilityModifierSnapshot mods;
        private float tickSec;

        public AlchemistAbilityObject(Player owner, AlchemistAbilityWorldEffect effect, float time)
            : this(owner, effect, time, AbilityModifierSnapshot.Empty)
        {
        }

        public AlchemistAbilityObject(Player owner, AlchemistAbilityWorldEffect effect, float time, AbilityModifierSnapshot mods)
        {
            this.owner = owner;
            this.effect = effect;
            this.mods = mods;
            position.Value = effect.target;
            radius = AbilityFunctions.Alchemist.GetRadius(effect.rage) + mods.abilityRadiusBonus;
            startTime = time + AbilityFunctions.Alchemist.Air_Time;
            float durationMul = mods.durationMul > 0 ? mods.durationMul : 1f;
            endTime = startTime + AbilityFunctions.Alchemist.GetGroundDurationMs(effect.rage) / 1000f * durationMul;
            tickSec = Math.Max(0.2f, (mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : 1000) / 1000f);
            nextTick = startTime;
            damage = (int)((effect.rage + effect.attack) * (1f + mods.abilityDamagePct));
        }

        public override bool CanShowTo(Player player)
        {
            return false;
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

            if (time.totalTime < nextTick) return;
            uint now = (uint)(time.totalTime * 1000);
            int attackAmt = 4 + mods.timedAttack;

            foreach (var player in world.objects.GetPlayersWithin(position.Value.x, position.Value.y, radius).ToArray())
                player.gameState.playerState.ApplyTimedStatBonus(StatType.Attack, attackAmt, now, 1050);

            foreach (var enemy in world.objects.GetEnemiesWithin(position.Value.x, position.Value.y, radius).ToArray())
            {
                int damageTaken = 0;
                owner.gameState.playerState.ability.TriggerTalisman(TalismanTrigger.AbilityTick, now, position.Value, enemy.position.Value, ref damageTaken, enemy);
                damageTaken = enemy.GetDamageTaken(damage);
                enemy.Hurt(damageTaken, owner);
                enemy.ServerDamage(damage, owner.info);
                owner.OnDamageEnemy(enemy, damageTaken);
                if (mods.slowMs > 0)
                    enemy.AddEffect(StatusEffect.Slowed, mods.slowMs / 1000f);
                if (enemy.GetHealth() <= 0)
                {
                    world.PushTickAction(() =>
                    {
                        enemy.Die(owner);
                    });
                }
            }

            nextTick = (float)time.totalTime + tickSec;
        }
    }
}
