using System;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;
using World.Map.Objects.Abilities;

namespace World.Abilities
{
    public class MinisterAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Minister;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            sendToSelf = false;
            failedToUse = false;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;

            var cost = AbilityFunctions.Minister.GetRageCost(rage);
            rageCost = cost;
            if (rage < cost)
            {
                failedToUse = true;
                return null;
            }

            ushort pillarType = 0xa2f;
            if (cost >= 100)
                pillarType = 0xa9c;
            else if (cost >= 75)
                pillarType = 0xa9b;
            else if (cost >= 50)
                pillarType = 0xa9a;

            int heal = AbilityFunctions.Minister.GetHealAmount(cost, attack);
            heal = (int)(heal * (1f + mods.healPower));
            float radius = AbilityFunctions.Minister.GetPillarRadius(cost) + mods.abilityRadiusBonus;
            int durationMs = AbilityFunctions.Minister.GetPillarDurationMs(cost) + Math.Max(0, mods.durationBonusMs);
            float tickSec = Math.Max(0.4f, (mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : 2000) / 1000f);

            var pillar = new MinisterPillar(player, heal, radius, durationMs / 1000f, tickSec, mods, (float)player.world.time.totalTime);
            pillar.position.Value = position;
            pillar.Initialize(GameData.objects[pillarType]);
            player.world.objects.SpawnObject(pillar);

            var worldEffectPacket = PlayColored(new MinisterAbilityWorldEffect(player.gameId, position, cost, attack));
            rage -= cost;
            return worldEffectPacket;
        }
    }
}
