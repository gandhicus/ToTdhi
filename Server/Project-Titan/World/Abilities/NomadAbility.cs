using System;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;
using World.Map.Objects.Abilities;
using World.Map.Objects.Entities;

namespace World.Abilities
{
    public class NomadAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Nomad;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
            bool marked = entity.currentSnapshot.HasServerEffect(StatusEffect.Marked);
            if (!marked && player.world.objects.TryGetEnemy(entity.gameId, out var live) && live.HasServerEffect(StatusEffect.Marked))
                marked = true;
            if (!marked) return;

            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            damageTaken = (int)(damageTaken * (1.15f + mods.markedDamagePct));
            int rage = 2 + mods.markedRage;
            if (rage > 0)
                PlayerState.AddRage(time, rage, false);
            TriggerTalisman(TalismanTrigger.HitMarked, time, player.position.Value, entity.GetPosition(time), ref damageTaken);
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            failedToUse = false;
            rageCost = AbilityFunctions.Nomad.Ability_Cost;
            sendToSelf = false;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;

            if (rage < rageCost)
            {
                failedToUse = true;
                return null;
            }

            float lifetime = 15f + mods.durationBonusMs / 1000f;
            var charm = new NomadCharm((float)player.world.time.totalTime, lifetime, player, mods);
            charm.position.Value = target;
            charm.Initialize(GameData.objects[0xa9d]);
            player.world.objects.SpawnObject(charm, AbilityFunctions.Nomad.Charm_Air_Time);

            rage -= AbilityFunctions.Nomad.Ability_Cost;
            return PlayColored(new NomadAbilityWorldEffect(player.gameId, target));
        }
    }
}
