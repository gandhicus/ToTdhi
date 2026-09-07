using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Components.Projectiles;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Models;
using Utils.NET.Utils;
using World.GameState;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class DpsCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "dps";

        public override string Syntax => "/dps {optional weapon name}";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            WeaponInfo weaponInfo;
            float enchantMod = 1f;

            if (args.args.Length == 0)
            {
                if (player.gameState?.playerState == null)
                    return ChatData.Error("Unable to read your stats right now.");

                var item = player.gameState.playerState.currentSnapshot.equips[0];
                if (item.IsBlank)
                    return ChatData.Error("You do not have a weapon equipped.");

                if (!(item.GetInfo() is WeaponInfo equippedWeapon))
                    return ChatData.Error("Your equipped item is not a weapon.");

                weaponInfo = equippedWeapon;
                if (item.enchantType == ItemEnchantType.Damaging)
                    enchantMod = EnchantFunctions.Damage(item.enchantLevel);
            }
            else
            {
                var name = StringUtils.ComponentsToString(' ', args.args);
                var info = GameData.GetObjectByName(name);

                if (info == null)
                {
                    var search = GameData.Search(name).Where(obj => obj is WeaponInfo).ToArray();
                    if (search.Length != 1)
                    {
                        var builder = new StringBuilder();
                        builder.Append("Unable to find weapon: " + name);
                        if (search.Length > 1 && search.Length <= 10)
                        {
                            builder.Append("\nDid you mean:");
                            foreach (var obj in search)
                            {
                                builder.Append('\n');
                                builder.Append(obj.name);
                            }
                        }
                        if (builder.Length >= NetConstants.Max_Chat_Length)
                            return ChatData.Error("Results are too large");
                        return ChatData.Error(builder.ToString());
                    }
                    info = search[0];
                }

                if (!(info is WeaponInfo foundWeapon))
                    return ChatData.Error($"'{info.name}' is not a weapon!");

                weaponInfo = foundWeapon;
            }

            if (player.gameState?.playerState == null)
                return ChatData.Error("Unable to read your stats right now.");

            if (weaponInfo.projectiles == null || weaponInfo.projectiles.Length == 0)
                return ChatData.Error("That weapon has no projectiles.");

            var playerState = player.gameState.playerState;
            var snapshot = playerState.currentSnapshot;
            var lookupByName = args.args.Length > 0;

            var equips = BuildEquips(snapshot, weaponInfo, lookupByName);
            var attack = GetAttack(snapshot, equips);
            var attackMod = StatFunctions.AttackModifier(attack, false);

            var trueDamageChance = ItemFunctions.GetEquippedAlternateStat(equips, AlternateStatType.TrueDamageChance)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.TrueDamageChance);
            var critChance = ItemFunctions.GetEquippedAlternateStat(equips, AlternateStatType.CriticalStrikeChance)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.CriticalStrikeChance);
            var critDamageBonus = ItemFunctions.GetEquippedAlternateStat(equips, AlternateStatType.CriticalStrikeDamage)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.CriticalStrikeDamage);
            var rofBonus = ItemFunctions.GetEquippedAlternateStat(equips, AlternateStatType.RateOfFire)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.RateOfFire);

            // Use the same cooldown rounding as live shooting so RoF gear matches what you actually fire.
            int cooldownMs = StatFunctions.GetShootCooldownMs(weaponInfo.rateOfFire, rofBonus);
            float shotsPerSecond = 1000f / cooldownMs;

            var cycle = WeaponFunctions.GetFireCycle(weaponInfo.projectiles);
            if (cycle.Count == 0)
                return ChatData.Error("That weapon has no projectiles.");

            player.AddChat(ChatData.Info(weaponInfo.name));
            player.AddChat(ChatData.Info($"Attack {attack}: {FormatNumber(attackMod)}x ({StatFunctions.Attack_Modifier_Base:0.##} + Attack/{StatFunctions.Attack_Modifier_Divisor:0})"));
            player.AddChat(ChatData.Info($"DPS at 0 Defense: {FormatDps(weaponInfo, cycle, attackMod, enchantMod, 0, shotsPerSecond, trueDamageChance, critChance, critDamageBonus)}"));
            player.AddChat(ChatData.Info($"DPS at 20 Defense: {FormatDps(weaponInfo, cycle, attackMod, enchantMod, 20, shotsPerSecond, trueDamageChance, critChance, critDamageBonus)}"));
            player.AddChat(ChatData.Info($"DPS at 40 Defense: {FormatDps(weaponInfo, cycle, attackMod, enchantMod, 40, shotsPerSecond, trueDamageChance, critChance, critDamageBonus)}"));
            player.AddChat(ChatData.Info($"Range: {FormatCycleRange(cycle)}"));
            player.AddChat(ChatData.Info($"Projectile count: {FormatProjectileCount(cycle)}"));
            player.AddChat(ChatData.Info(FormatRateOfFire(shotsPerSecond, weaponInfo.rateOfFire)));

            return null;
        }

        private static Item[] BuildEquips(PlayerSnapshot snapshot, WeaponInfo weaponInfo, bool swapWeapon)
        {
            var equips = new Item[snapshot.equips.Length];
            Array.Copy(snapshot.equips, equips, equips.Length);
            if (swapWeapon)
                equips[0] = new Item(weaponInfo.id);
            return equips;
        }

        private static int GetAttack(PlayerSnapshot snapshot, Item[] equips)
        {
            var extraStats = new Dictionary<StatType, int>();
            EquipmentStatFunctions.RecalculateEquipmentStats(equips, extraStats, new Dictionary<AlternateStatType, int>());
            extraStats.TryGetValue(StatType.Attack, out var equipAttack);
            return snapshot.GetBaseStat(StatType.Attack) + snapshot.attackBonus + equipAttack;
        }

        // Defense is subtracted per hit, so a 2-projectile volley into 40 Defense is
        // 2x (damage-40), not (2x damage)-40. Average min/max after that expected value.
        private static string FormatDps(
            WeaponInfo weapon,
            List<ProjectileData[]> cycle,
            float attackMod,
            float enchantMod,
            int defense,
            float shotsPerSecond,
            int trueDamageChance,
            int critChance,
            int critDamageBonus)
        {
            float minExpected = 0;
            float maxExpected = 0;
            foreach (var shot in cycle)
            {
                foreach (var proj in shot)
                {
                    WeaponFunctions.GetProjectileDamage(weapon.slotType, proj, out var min, out var max);
                    minExpected += ExpectedDamage(ScaleDamage(min, attackMod, enchantMod), trueDamageChance, critChance, critDamageBonus, defense);
                    maxExpected += ExpectedDamage(ScaleDamage(max, attackMod, enchantMod), trueDamageChance, critChance, critDamageBonus, defense);
                }
            }

            var dps = ((minExpected + maxExpected) / 2f / cycle.Count) * shotsPerSecond;
            return FormatNumber(dps);
        }

        private static int ScaleDamage(ushort baseDamage, float attackMod, float enchantMod)
        {
            return (int)Math.Round(baseDamage * attackMod * enchantMod);
        }

        private static float ExpectedDamage(int rawDamage, int trueDamageChance, int critChance, int critDamageBonus, int defense)
        {
            var pCrit = ClampPercent(critChance) / 100f;
            var pTrue = ClampPercent(trueDamageChance) / 100f;
            var critMult = StatFunctions.CriticalStrikeMultiplier(critDamageBonus);

            float ExpectedForRaw(int raw)
            {
                var critDamage = (int)(raw * critMult);

                float ExpectedForDamage(int damage)
                {
                    var mitigated = StatFunctions.DamageTaken(defense, damage, false);
                    return (1f - pTrue) * mitigated + pTrue * damage;
                }

                return (1f - pCrit) * ExpectedForDamage(raw) + pCrit * ExpectedForDamage(critDamage);
            }

            return ExpectedForRaw(rawDamage);
        }

        private static int ClampPercent(int value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return value;
        }

        private static string FormatCycleRange(List<ProjectileData[]> cycle)
        {
            var ranges = new List<string>();
            foreach (var shot in cycle)
            {
                foreach (var proj in shot)
                {
                    var range = FormatRange(proj);
                    if (!ranges.Contains(range))
                        ranges.Add(range);
                }
            }

            if (ranges.Count == 0)
                return "0";
            return string.Join(" / ", ranges);
        }

        private static string FormatProjectileCount(List<ProjectileData[]> cycle)
        {
            var counts = new int[cycle.Count];
            for (int i = 0; i < cycle.Count; i++)
                counts[i] = cycle[i].Length;

            if (counts.Length == 0)
                return "0";
            if (counts.All(count => count == counts[0]))
                return counts[0].ToString();
            return string.Join(" / ", counts);
        }

        private static string FormatRateOfFire(float shotsPerSecond, float baseRateOfFire)
        {
            if (Math.Abs(shotsPerSecond - baseRateOfFire) < 0.05f)
                return $"Rate of fire: {FormatNumber(shotsPerSecond)}";
            return $"Rate of fire: {FormatNumber(shotsPerSecond)} (base {FormatNumber(baseRateOfFire)})";
        }

        private static string FormatRange(ProjectileData shot)
        {
            if (shot is AoeProjectileData aoeData)
                return FormatNumber(aoeData.range);

            return FormatNumber((int)Math.Round(shot.lifetime * shot.speed * 100) / 100f);
        }

        private static string FormatNumber(float value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.05f)
                return ((int)Math.Round(value)).ToString();
            return value.ToString("0.#");
        }
    }
}
