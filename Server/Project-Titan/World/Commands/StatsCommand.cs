using System;
using TitanCore.Core;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class StatsCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "stats";

        public override string Syntax => "/stats";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (player.gameState?.playerState == null)
                return ChatData.Error("Unable to read your stats right now.");

            var playerState = player.gameState.playerState;
            var snapshot = playerState.currentSnapshot;
            var time = playerState.LastClientTime;

            var attack = snapshot.GetFunctionalStat(StatType.Attack);
            var defense = snapshot.GetFunctionalStat(StatType.Defense);
            var speed = snapshot.GetFunctionalStat(StatType.Speed);
            var vigor = snapshot.GetFunctionalStat(StatType.Vigor);

            var damaging = playerState.HasEffect(StatusEffect.Damaging, time);
            var fortified = playerState.HasEffect(StatusEffect.Fortified, time);
            var slowed = playerState.HasEffect(StatusEffect.Slowed, time);
            var speedy = playerState.HasEffect(StatusEffect.Speedy, time);
            var healing = playerState.HasEffect(StatusEffect.Healing, time);
            var sick = playerState.HasEffect(StatusEffect.Sick, time);
            var defenseMinus = playerState.HasEffect(StatusEffect.DefenseMinus, time)
                ? player.GetDefenseMinusAmount()
                : 0;

            var attackBonus = StatFunctions.AttackDamageBonusPercent(attack, damaging);
            var attackMod = StatFunctions.AttackModifier(attack, damaging);
            var tilesPerSecond = StatFunctions.TilesPerSecond(speed, slowed, speedy);
            var healthPerSecond = StatFunctions.HealthRegenPerSecond(vigor, healing, sick);

            var critChance = snapshot.GetAlternateStat(AlternateStatType.CriticalStrikeChance)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.CriticalStrikeChance, time);
            var critDamage = snapshot.GetAlternateStat(AlternateStatType.CriticalStrikeDamage)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.CriticalStrikeDamage, time);
            var trueDamage = snapshot.GetAlternateStat(AlternateStatType.TrueDamageChance)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.TrueDamageChance, time);
            var blockChance = snapshot.GetAlternateStat(AlternateStatType.BlockChance)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.BlockChance, time);
            var absorptionChance = snapshot.GetAlternateStat(AlternateStatType.AbsorptionChance)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.AbsorptionChance, time);
            var rofBonus = snapshot.GetAlternateStat(AlternateStatType.RateOfFire)
                + playerState.GetTimedAlternateStatBonus(AlternateStatType.RateOfFire, time);

            player.AddChat(ChatData.Info($"Attack {attack}: {FormatNumber(attackMod)}x weapon damage ({FormatAttackFormula(damaging)})"));
            player.AddChat(ChatData.Info($"Attack damage bonus: {FormatPercent(attackBonus)}"));
            player.AddChat(ChatData.Info($"Tiles/sec: {FormatNumber(tilesPerSecond)}"));
            player.AddChat(ChatData.Info($"HP/sec: {FormatNumber(healthPerSecond)}"));
            player.AddChat(ChatData.Info($"Defense negation from a 50 damage shot: {FormatPercent(StatFunctions.DefenseNegationPercent(defense, 50, fortified, defenseMinus))}"));
            player.AddChat(ChatData.Info($"Defense negation from a 100 damage shot: {FormatPercent(StatFunctions.DefenseNegationPercent(defense, 100, fortified, defenseMinus))}"));
            player.AddChat(ChatData.Info($"Defense negation from a 150 damage shot: {FormatPercent(StatFunctions.DefenseNegationPercent(defense, 150, fortified, defenseMinus))}"));
            player.AddChat(ChatData.Info($"Crit chance: {FormatPercent(critChance)} | Crit damage bonus: {FormatPercent(critDamage)} | True damage: {FormatPercent(trueDamage)}"));
            player.AddChat(ChatData.Info($"Block: {FormatPercent(blockChance)} | Absorption: {FormatPercent(absorptionChance)} | RoF bonus: {FormatPercent(rofBonus)}"));

            return null;
        }

        private static string FormatAttackFormula(bool damaging)
        {
            var formula = $"{StatFunctions.Attack_Modifier_Base:0.##} + Attack/{StatFunctions.Attack_Modifier_Divisor:0}";
            return damaging ? formula + ", x1.5 Damaging" : formula;
        }

        private static string FormatPercent(float value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.05f)
                return $"{(int)Math.Round(value)}%";
            return $"{value:0.#}%";
        }

        private static string FormatNumber(float value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.05f)
                return ((int)Math.Round(value)).ToString();
            return value.ToString("0.#");
        }
    }
}
