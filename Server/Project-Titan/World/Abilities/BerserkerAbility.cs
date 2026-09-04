using System;
using TitanCore.Core;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using World.GameState;

namespace World.Abilities
{
    public class BerserkerAbility : ClassAbility
    {
        public override ClassType ClassType => ClassType.Berserker;

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
            byte spent = rage;
            SpendDumpRage(ref rage, mods, out rageCost);
            SkillTreeFunctions.ApplyRageToOnUseStats(ref mods, spent);

            float shoutSpread = AbilityFunctions.Berserker.GetShoutSpread(spent, attack) + mods.shoutSpreadDeg * AngleUtils.PI / 180f;
            float shoutRadius = AbilityFunctions.Berserker.GetShoutRange(spent, attack) + mods.abilityRangeBonus;
            float shoutAngle = position.AngleTo(target);
            float slowSec = 5f + mods.slowMs / 1000f;
            int weaponDamage = player.GetHeldWeaponShotDamage(time, time);
            int shoutDamage = AbilityFunctions.Berserker.ScaleWeaponDamage(weaponDamage);
            shoutDamage = AbilityFunctions.RageSpend.ApplyRageDamageMul(shoutDamage, spent, AbilityFunctions.Berserker.Rage_Damage_At_100);
            shoutDamage = Math.Max(1, (int)(shoutDamage * (1f + mods.abilityDamagePct)));

            foreach (var enemy in player.world.objects.GetEnemiesWithin(position.x, position.y, shoutRadius))
            {
                if (Math.Abs(AngleUtils.Difference(position.AngleTo(enemy.position.Value), shoutAngle)) > shoutSpread / 2)
                    continue;
                enemy.AddEffect(StatusEffect.Slowed, slowSec);
                if (shoutDamage > 0)
                {
                    var damageTaken = enemy.GetDamageTaken(shoutDamage);
                    enemy.Hurt(damageTaken, player);
                    enemy.ServerDamage(damageTaken, player.info);
                    player.OnDamageEnemy(enemy, damageTaken);
                    TriggerTalisman(TalismanTrigger.AbilityHit, time, position, enemy.position.Value, ref damageTaken);
                    if (enemy.GetHealth() <= 0)
                        enemy.Die(player);
                }
            }

            uint rofMs = AbilityFunctions.Berserker.GetRoFDurationMs(spent, attack) + (uint)Math.Max(0, mods.durationBonusMs);
            float rofArea = AbilityFunctions.Berserker.GetRoFArea(spent, attack);
            int rofAmt = AbilityFunctions.Berserker.RoF_Amount + mods.rofAmount;
            foreach (var other in player.world.objects.GetPlayersWithin(position.x, position.y, rofArea))
                other.gameState.playerState.ApplyTimedAlternateStatBonus(AlternateStatType.RateOfFire, rofAmt, time, rofMs);

            if (mods.timedAttack > 0 && mods.timedAttackMs > 0)
                PlayerState.ApplyTimedStatBonus(StatType.Attack, mods.timedAttack, time, (uint)mods.timedAttackMs);

            return PlayColored(new BerserkerAbilityWorldEffect(player.gameId, position, shoutAngle * AngleUtils.Rad2Deg, spent, attack));
        }
    }
}
