using System;
using System.Collections.Generic;
using TitanCore.Core;
using TitanCore.Data.Items;
using TitanCore.Net.Packets.Models;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;

namespace World.Abilities
{
    public class LancerAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Lancer;

        public override void OnHit(EntityState entity, uint time, ref int damageTaken)
        {
        }

        public override void OnHit(EntityState entity, uint time, ref int damageTaken, ushort sourceItem)
        {
            if (sourceItem != AbilityFunctions.Lancer.Ability_Item_Id) return;

            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            TriggerTalisman(TalismanTrigger.AbilityHit, time, player.position.Value, entity.GetPosition(time), ref damageTaken);
            if (mods.rageOnKill > 0 && player.world.objects.TryGetEnemy(entity.gameId, out var enemy) && enemy.GetHealth() - damageTaken <= 0)
                PlayerState.AddRage(time, mods.rageOnKill, false);
        }

        public override void OnMove(Vec2 position, uint time)
        {
        }

        public override TnPlayEffect UseAbility(uint time, Vec2 position, Vec2 target, byte value, int attack, ref byte rage, out byte rageCost, out bool sendToSelf, out bool failedToUse)
        {
            sendToSelf = false;
            failedToUse = false;
            var mods = SkillTreeFunctions.IsEnabled ? PlayerState.abilityMods : AbilityModifierSnapshot.Empty;
            int cost = AbilityFunctions.RageSpend.GetLancerRageCost(mods);
            rageCost = (byte)cost;
            int rageForDamage = rage;

            var lancerItem = new Item(AbilityFunctions.Lancer.Ability_Item_Id);
            var lancerWeaponInfo = (WeaponInfo)lancerItem.GetInfo();
            var lancerProjData = lancerWeaponInfo.projectiles[0];
            float aim = position.AngleTo(target);

            var spawned = new List<AllyProjectile>();
            foreach (var angle in AbilityFunctions.Lancer.GetNovaAngles(aim))
            {
                var batch = player.GetProjectiles(lancerItem, lancerProjData, lancerWeaponInfo, player.projIds, player.gameId, angle, false, time);
                spawned.AddRange(batch);
            }

            var projectiles = spawned.ToArray();
            for (int i = 0; i < projectiles.Length; i++)
            {
                var proj = projectiles[i];
                int weaponDamage = player.GetHeldWeaponShotDamage(proj.projectileId, time);
                int damage = AbilityFunctions.Lancer.ScaleWeaponDamage(weaponDamage);
                damage = AbilityFunctions.RageSpend.ApplyRageDamageMul(damage, rageForDamage);
                damage = (int)(damage * (1f + mods.abilityDamagePct));
                proj.damage = (ushort)Math.Max(1, damage);
                projectiles[i] = proj;
            }

            player.gameState.AddPlayerProjectiles(time, position, projectiles);
            foreach (var otherPlayer in player.playersSentTo)
                if (player != otherPlayer)
                    otherPlayer.gameState.AddAllyProjectiles(projectiles);

            if (mods.timedAttack > 0 && mods.timedAttackMs > 0)
            {
                SkillTreeFunctions.ApplyRageToOnUseStats(ref mods, rage);
                PlayerState.ApplyTimedStatBonus(StatType.Attack, mods.timedAttack, time, (uint)mods.timedAttackMs);
            }

            rage = (byte)Math.Max(0, rage - cost);
            return null;
        }
    }
}
