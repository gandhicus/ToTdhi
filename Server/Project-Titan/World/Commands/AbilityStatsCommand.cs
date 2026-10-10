using System;
using TitanCore.Core;
using TitanCore.Data.Components;
using TitanCore.Data.Components.Projectiles;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Models;
using World.GameState;
using World.Map.Objects.Entities;

namespace World.Commands
{
    /// <summary>
    /// Prints live ability numbers for the player's class.
    /// Each fact is its own chat line so we stay under the 120-character message cap.
    /// </summary>
    public class AbilityStatsCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Player;

        public override string Command => "abilitystats";

        public override string Syntax => "/abilitystats {optional rage 1-100}";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (player.gameState?.playerState == null)
                return ChatData.Error("Unable to read your stats right now.");

            var playerState = player.gameState.playerState;
            var snapshot = playerState.currentSnapshot;
            var time = playerState.LastClientTime;
            var classType = (ClassType)player.info.id;
            int attack = snapshot.GetFunctionalStat(StatType.Attack);
            int liveRage = AbilityFunctions.RageSpend.GetIntegralRage(playerState.rage);
            var mods = SkillTreeFunctions.IsEnabled ? playerState.abilityMods : AbilityModifierSnapshot.Empty;

            // Optional rage override lets you inspect a dump without filling the bar.
            // With 0 rage we still show a full dump so the command is useful in town.
            int rage = liveRage;
            bool assumedFullDump = false;
            if (args.args.Length > 0)
            {
                if (!int.TryParse(args.args[0], out rage) || rage < 0 || rage > 100)
                    return SyntaxError;
            }
            else if (liveRage <= 0)
            {
                rage = classType == ClassType.Bladeweaver
                    ? AbilityFunctions.BladeWeaver.Max_Dash_Rage
                    : 100;
                assumedFullDump = true;
            }

            Line(player, $"{player.info.name} ability | attack {attack} | rage {rage}{(assumedFullDump ? " (you have 0)" : liveRage != rage ? $" (you have {liveRage})" : "")}");

            int cooldownMs = playerState.GetAbilityCooldownMs((byte)rage);
            Line(player, $"Cooldown: {FormatSec(cooldownMs)}");

            switch (classType)
            {
                case ClassType.Ranger:
                    WriteRanger(player, playerState, mods, rage, attack, time);
                    break;
                case ClassType.Warrior:
                    WriteWarrior(player, playerState, mods, rage, attack, time);
                    break;
                case ClassType.Commander:
                    WriteCommander(player, mods, rage, attack);
                    break;
                case ClassType.Lancer:
                    WriteLancer(player, playerState, mods, rage, time);
                    break;
                case ClassType.Alchemist:
                    WriteAlchemist(player, playerState, mods, rage, time);
                    break;
                case ClassType.Berserker:
                    WriteBerserker(player, playerState, mods, rage, attack, time);
                    break;
                case ClassType.Nomad:
                    WriteNomad(player, mods, rage);
                    break;
                case ClassType.Bladeweaver:
                    WriteBladeweaver(player, playerState, mods, rage, time);
                    break;
                case ClassType.Minister:
                    WriteMinister(player, mods, rage, attack);
                    break;
                case ClassType.Brewer:
                    WriteBrewer(player, mods, rage);
                    break;
                default:
                    Line(player, "This class has no ability stats.");
                    break;
            }

            // Bladeweaver on-use stats (Alacrity) scale with dash rage, not the full bar.
            int onUseRage = classType == ClassType.Bladeweaver
                ? Math.Min(rage, AbilityFunctions.BladeWeaver.Max_Dash_Rage)
                : rage;
            WriteOnUseStats(player, classType, mods, onUseRage);
            WriteTalismanOnUse(player, mods, rage);
            return null;
        }

        private static void WriteRanger(Player player, PlayerState playerState, AbilityModifierSnapshot mods, int rage, int attack, uint time)
        {
            WriteDumpRageCost(player, mods, rage);
            GetHeldWeapon(playerState, out var weapon);
            bool damaging = playerState.HasEffect(StatusEffect.Damaging, time);
            if (TryGetWeaponVolleyOutgoing(playerState, time, out var minDmg, out var maxDmg) && weapon != null)
            {
                byte spent = DumpSpent(mods, rage);
                byte spentFull = DumpSpent(mods, 100);
                Line(player, $"Bow volley: {FormatRangeNum((int)Math.Round(minDmg), (int)Math.Round(maxDmg))}");

                int rainMin = ScaleRanger(minDmg, mods.abilityDamagePct, spent, attack, damaging);
                int rainMax = ScaleRanger(maxDmg, mods.abilityDamagePct, spent, attack, damaging);
                int rainMinFull = ScaleRanger(minDmg, mods.abilityDamagePct, spentFull, attack, damaging);
                int rainMaxFull = ScaleRanger(maxDmg, mods.abilityDamagePct, spentFull, attack, damaging);
                Pair(player, "Rain hit damage", FormatRangeNum(rainMin, rainMax), FormatRangeNum(rainMinFull, rainMaxFull), spent);

                float vsNow = RangerVsDisplayedVolley(spent, attack, damaging);
                float vsFull = RangerVsDisplayedVolley(100, attack, damaging);
                string attackShare = $"{AbilityFunctions.Ranger.Attack_Scale * 100f:0}% Attack scaling";
                if (spent >= 100)
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x bow volley ({attackShare}, {FormatNumber(AbilityFunctions.Ranger.Weapon_Damage_Mul * AbilityFunctions.Ranger.Rage_Damage_At_100)}x raw at 100 rage)");
                else
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x volley ({FormatNumber(vsFull)}x at 100 rage, {attackShare})");
            }
            else
                Line(player, "Rain damage: equip a weapon to see ability damage");

            Pair(player, "Radius", Tiles(AbilityFunctions.Ranger.GetRadiusWithBonus(rage, attack, mods.abilityRadiusBonus)),
                Tiles(AbilityFunctions.Ranger.GetRadiusWithBonus(100, attack, mods.abilityRadiusBonus)), rage);
            Line(player, $"Cast range: {Tiles(6f + mods.abilityRangeBonus)}");
            if (mods.slowMs > 0)
                Line(player, $"Applies Slowed for {FormatSec(mods.slowMs)}");
            if (mods.rageOnKill > 0)
                Line(player, $"Rage on kill: {mods.rageOnKill}");
        }

        private static void WriteWarrior(Player player, PlayerState playerState, AbilityModifierSnapshot mods, int rage, int attack, uint time)
        {
            WriteDumpRageCost(player, mods, rage);
            byte spent = DumpSpent(mods, rage);
            int heal = AbilityFunctions.Warrior.GetHealAmount(spent, attack);
            if (mods.healPower != 0)
                heal = Math.Max(0, (int)(heal * (1f + mods.healPower)));
            byte spentFull = DumpSpent(mods, 100);
            int healFull = AbilityFunctions.Warrior.GetHealAmount(spentFull, attack);
            if (mods.healPower != 0)
                healFull = Math.Max(0, (int)(healFull * (1f + mods.healPower)));
            Pair(player, "Heal per pulse", Num(heal), Num(healFull), rage);

            uint duration = AbilityFunctions.Warrior.GetAbilityDuration(spent) + (uint)Math.Max(0, mods.durationBonusMs);
            int lockout = mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : SkillTreeFunctions.Base_Pulse_Lockout_Ms;
            Line(player, $"Duration: {FormatSec((int)duration)} | Pulse lockout: {FormatSec(lockout)} (on weapon hit)");
            Line(player, $"Heal radius: {Tiles(AbilityFunctions.Warrior.Heal_Area)}");

            if (mods.weaponDamagePct > 0)
            {
                // Cleave uses raw weapon min/max; GetCleaveOutgoing applies Attack itself.
                if (TryGetWeaponRaw(playerState, out var minRaw, out var maxRaw))
                {
                    bool damaging = playerState.HasEffect(StatusEffect.Damaging, time);
                    int cleaveMin = AbilityFunctions.Warrior.GetCleaveOutgoing(minRaw, minRaw, attack, damaging, mods.weaponDamagePct);
                    int cleaveMax = AbilityFunctions.Warrior.GetCleaveOutgoing(maxRaw, maxRaw, attack, damaging, mods.weaponDamagePct);
                    Line(player, $"Cleave on pulse: {FormatRangeNum(cleaveMin, cleaveMax)} outgoing");
                }
                else
                    Line(player, "Cleave on pulse: equip a weapon to see damage");
            }
        }

        private static void WriteCommander(Player player, AbilityModifierSnapshot mods, int rage, int attack)
        {
            WriteDumpRageCost(player, mods, rage);
            float rageScalar = rage / 100f;
            float attackScalar = 0.5f + attack / 50f;
            uint defDuration = (uint)(AbilityFunctions.Commander.GetDefenseDurationMs(rage, attack) * (mods.durationMul > 0 ? mods.durationMul : 1f));
            uint defFull = (uint)(AbilityFunctions.Commander.GetDefenseDurationMs(100, attack) * (mods.durationMul > 0 ? mods.durationMul : 1f));
            int defenseBonus = AbilityFunctions.Commander.GetDefenseBonus(attack, rage);
            int defenseBonusFull = AbilityFunctions.Commander.GetDefenseBonus(attack, 100);
            Pair(player, "Defense bonus", $"+{defenseBonus}", $"+{defenseBonusFull}", rage);
            Pair(player, "Defense duration", FormatSec((int)defDuration), FormatSec((int)defFull), rage);

            // Match Brandish's multiplier on the full Reach duration used by the ability.
            float reachDurationMul = 1f + Math.Max(0f, mods.reachDurationPct);
            uint rangeDuration = (uint)((2500 + 11000 * rageScalar * attackScalar) * reachDurationMul);
            uint rangeFull = (uint)((2500 + 11000 * attackScalar) * reachDurationMul);
            Pair(player, "Reach duration", FormatSec((int)rangeDuration), FormatSec((int)rangeFull), rage);
            Pair(player, "Reach radius", Tiles(2.5f + 6f * rageScalar + mods.abilityRangeBonus), Tiles(2.5f + 6f + mods.abilityRangeBonus), rage);
        }

        private static void WriteLancer(Player player, PlayerState playerState, AbilityModifierSnapshot mods, int rage, uint time)
        {
            int cost = AbilityFunctions.RageSpend.GetLancerRageCost(mods);
            Line(player, $"Rage cost: {cost} (damage uses rage on bar, not the cost)");
            Line(player, $"Nova: {AbilityFunctions.Lancer.Nova_Count} projectiles, {AbilityFunctions.Lancer.Nova_Hits_Per_Target} hits per target");

            if (TryGetWeaponOutgoing(playerState, time, out var minDmg, out var maxDmg))
            {
                int projMin = ScaleLancer(minDmg, mods.abilityDamagePct, rage);
                int projMax = ScaleLancer(maxDmg, mods.abilityDamagePct, rage);
                int projMinFull = ScaleLancer(minDmg, mods.abilityDamagePct, 100);
                int projMaxFull = ScaleLancer(maxDmg, mods.abilityDamagePct, 100);
                int hits = AbilityFunctions.Lancer.Nova_Hits_Per_Target;
                Pair(player, "Damage/projectile", FormatRangeNum(projMin, projMax), FormatRangeNum(projMinFull, projMaxFull), rage);
                Pair(player, "Damage/target", FormatRangeNum(projMin * hits, projMax * hits), FormatRangeNum(projMinFull * hits, projMaxFull * hits), rage);
                float vsNow = LancerWeaponShotMul(rage);
                float vsFull = LancerWeaponShotMul(100);
                if (rage >= 100)
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x a spear shot (2x weapon x 3x at 100 rage, Wrath included)");
                else
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x spear ({FormatNumber(vsFull)}x at 100 rage, Wrath included)");
            }
            else
                Line(player, "Damage: equip a weapon to see ability damage");

            float range = 5f + mods.abilityRangeBonus;
            var lancerItem = new Item(AbilityFunctions.Lancer.Ability_Item_Id);
            if (lancerItem.GetInfo() is WeaponInfo lancerWeapon && lancerWeapon.projectiles != null && lancerWeapon.projectiles.Length > 0)
                range = ProjectileRange(lancerWeapon.projectiles[0]) + mods.abilityRangeBonus;
            Line(player, $"Range: {Tiles(range)}");
            if (mods.projectileSizePct > 0)
                Line(player, $"Size: +{Pct(mods.projectileSizePct)}");
            if (mods.pierceChance > 0)
                Line(player, $"Pierce chance: {mods.pierceChance}%");
        }

        private static void WriteAlchemist(Player player, PlayerState playerState, AbilityModifierSnapshot mods, int rage, uint time)
        {
            WriteDumpRageCost(player, mods, rage);
            GetHeldWeapon(playerState, out var weapon);
            if (TryGetWeaponOutgoing(playerState, time, out var minDmg, out var maxDmg) && weapon != null)
            {
                byte spent = DumpSpent(mods, rage);
                byte spentFull = DumpSpent(mods, 100);
                Line(player, $"Elixir shot: {FormatRangeNum((int)Math.Round(minDmg), (int)Math.Round(maxDmg))} (each tick uses 1 shot, not a volley)");

                int tickMin = ScaleAlchemist(minDmg, mods.abilityDamagePct, spent);
                int tickMax = ScaleAlchemist(maxDmg, mods.abilityDamagePct, spent);
                int tickMinFull = ScaleAlchemist(minDmg, mods.abilityDamagePct, spentFull);
                int tickMaxFull = ScaleAlchemist(maxDmg, mods.abilityDamagePct, spentFull);
                Pair(player, "Damage/tick", FormatRangeNum(tickMin, tickMax), FormatRangeNum(tickMinFull, tickMaxFull), spent);

                float vsNow = AlchemistWeaponShotMul(spent);
                float vsFull = AlchemistWeaponShotMul(100);
                if (spent >= 100)
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x elixir shot ({FormatNumber(AbilityFunctions.Alchemist.Weapon_Damage_Mul)}x at 100 rage)");
                else
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x shot ({FormatNumber(vsFull)}x at 100 rage)");
            }
            else
                Line(player, "Damage/tick: equip a weapon to see ability damage");

            float durationMul = mods.durationMul > 0 ? mods.durationMul : 1f;
            float duration = AbilityFunctions.Alchemist.GetGroundDurationMs((byte)rage) / 1000f * durationMul;
            float durationFull = AbilityFunctions.Alchemist.GetGroundDurationMs(100) / 1000f * durationMul;
            int tickMs = mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : 1000;
            Pair(player, "Duration", FormatSec((int)(duration * 1000)), FormatSec((int)(durationFull * 1000)), rage);
            Line(player, $"Tick every {FormatSec(Math.Max(200, tickMs))} after {FormatSec((int)(AbilityFunctions.Alchemist.Air_Time * 1000))} air time");
            Line(player, $"Radius: {Tiles(AbilityFunctions.Alchemist.GetRadius((byte)rage) + mods.abilityRadiusBonus)}");
            if (mods.slowMs > 0)
                Line(player, $"Applies Slowed for {FormatSec(mods.slowMs)} each tick");

            int puddleAttack = AbilityFunctions.Alchemist.Puddle_Attack + mods.fieldAttack;
            Line(player, $"Puddle Attack: +{puddleAttack} while standing in the puddle (base +{AbilityFunctions.Alchemist.Puddle_Attack}, Blight +{mods.fieldAttack})");
            if (mods.fieldSpeed > 0)
                Line(player, $"Puddle Speed: +{mods.fieldSpeed} while standing in the puddle (Alacrity)");
        }

        private static void WriteBerserker(Player player, PlayerState playerState, AbilityModifierSnapshot mods, int rage, int attack, uint time)
        {
            WriteDumpRageCost(player, mods, rage);
            GetHeldWeapon(playerState, out var weapon);
            if (TryGetWeaponVolleyOutgoing(playerState, time, out var minDmg, out var maxDmg) && weapon != null)
            {
                byte spent = DumpSpent(mods, rage);
                byte spentFull = DumpSpent(mods, 100);
                int shots = WeaponFunctions.GetVolleyShotCount(weapon.projectiles);
                string shotWord = shots == 1 ? "shot" : "shots";
                Line(player, $"Claymore volley: {FormatRangeNum((int)Math.Round(minDmg), (int)Math.Round(maxDmg))} ({shots} {shotWord} summed)");

                int shoutMin = ScaleBerserker(minDmg, mods.abilityDamagePct, spent);
                int shoutMax = ScaleBerserker(maxDmg, mods.abilityDamagePct, spent);
                int shoutMinFull = ScaleBerserker(minDmg, mods.abilityDamagePct, spentFull);
                int shoutMaxFull = ScaleBerserker(maxDmg, mods.abilityDamagePct, spentFull);
                Pair(player, "Shout damage", FormatRangeNum(shoutMin, shoutMax), FormatRangeNum(shoutMinFull, shoutMaxFull), spent);
                float vsNow = BerserkerWeaponShotMul(spent);
                float vsFull = BerserkerWeaponShotMul(100);
                if (spent >= 100)
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x claymore volley (30% weapon x 6.7x at 100 rage, Wrath included)");
                else
                    Line(player, $"Vs weapon: {FormatNumber(vsNow)}x volley ({FormatNumber(vsFull)}x at 100 rage, Wrath included)");
            }
            else
                Line(player, "Shout damage: equip a weapon to see ability damage");

            float coneDeg = AbilityFunctions.Berserker.GetShoutSpread(rage, attack) * 180f / (float)Math.PI + mods.shoutSpreadDeg;
            Line(player, $"Shout range: {Tiles(AbilityFunctions.Berserker.GetShoutRange(rage, attack) + mods.abilityRangeBonus)} | Cone: {FormatNumber(coneDeg)}°");
            float slowSec = AbilityFunctions.Berserker.GetShoutSlowDuration(rage, mods.slowDurationPct);
            float slowFull = AbilityFunctions.Berserker.GetShoutSlowDuration(100, mods.slowDurationPct);
            Pair(player, "Shout Slowed", $"{FormatNumber(slowSec)}s", $"{FormatNumber(slowFull)}s", rage);
            if (mods.slowDurationPct > 0f)
                Line(player, $"Enigma: +{Pct(mods.slowDurationPct)} Slowed duration");

            uint rofMs = AbilityFunctions.Berserker.GetRoFDurationMs(rage, attack) + (uint)Math.Max(0, mods.durationBonusMs);
            uint rofFull = AbilityFunctions.Berserker.GetRoFDurationMs(100, attack) + (uint)Math.Max(0, mods.durationBonusMs);
            int rofAmt = AbilityFunctions.Berserker.RoF_Amount + SkillTreeFunctions.ScaleOnUseStat(mods.rofAmount, rage);
            Pair(player, "Allies RoF", $"+{rofAmt}% for {FormatSec((int)rofMs)}",
                $"+{AbilityFunctions.Berserker.RoF_Amount + SkillTreeFunctions.ScaleOnUseStat(mods.rofAmount, 100)}% for {FormatSec((int)rofFull)}", rage);
            Pair(player, "RoF radius", Tiles(AbilityFunctions.Berserker.GetRoFArea(rage, attack)),
                Tiles(AbilityFunctions.Berserker.GetRoFArea(100, attack)), rage);
        }

        private static void WriteNomad(Player player, AbilityModifierSnapshot mods, int rage)
        {
            Line(player, $"Rage cost: {AbilityFunctions.Nomad.Ability_Cost}");
            float lifetime = 15f + mods.durationBonusMs / 1000f;
            Line(player, $"Charm lifetime: {FormatNumber(lifetime)}s | Mark radius: {Tiles(1f + mods.markRadiusBonus)}");
            Line(player, "One charm per Nomad; interacting removes its Marks");
            int linger = AbilityFunctions.Nomad.Marked_Linger_Ms + Math.Max(0, mods.markedLingerMs);
            Line(player, $"Marked linger: {FormatSec(linger)} | Marked hits: +{Pct(AbilityFunctions.Nomad.Marked_Hit_Mul - 1f + mods.markedDamagePct)} damage");
            if (mods.markedDefenseCap > 0)
                Line(player, $"Aegis: +1 Defense per Marked hit for 1s, up to +{mods.markedDefenseCap}");
            if (mods.markedRofCap > 0)
                Line(player, $"Flicker: +1% RoF per Marked hit for 1s, up to +{mods.markedRofCap}%");
            Line(player, $"Interact heal: {100 + mods.interactHealBonus} HP + 8 Vigor for 6s");
            int talismanRof = AbilityFunctions.Nomad.GetInteractTalismanRof(mods, rage);
            if (talismanRof > 0)
                Line(player, $"Owner interact talisman: +{talismanRof}% RoF for {FormatSec((int)AbilityFunctions.Nomad.Interact_Talisman_RoF_Duration_Ms)}");
        }

        private static void WriteBladeweaver(Player player, PlayerState playerState, AbilityModifierSnapshot mods, int rage, uint time)
        {
            int maxRage = AbilityFunctions.BladeWeaver.Max_Dash_Rage;
            int dashRage = Math.Min(rage, maxRage);
            Line(player, $"Rage cost: 1-{maxRage} (this dash {dashRage})");

            GetHeldWeapon(playerState, out var weapon);
            if (TryGetWeaponVolleyOutgoing(playerState, time, out var minDmg, out var maxDmg) && weapon != null)
            {
                int shots = WeaponFunctions.GetVolleyShotCount(weapon.projectiles);
                string shotWord = shots == 1 ? "shot" : "shots";
                Line(player, $"Sword volley: {FormatRangeNum((int)Math.Round(minDmg), (int)Math.Round(maxDmg))} ({shots} {shotWord} summed)");

                int minVolley = (int)Math.Round(minDmg);
                int maxVolley = (int)Math.Round(maxDmg);
                int adjustedMin = AbilityFunctions.BladeWeaver.AdjustVolleyDamage(minVolley, minVolley, maxVolley);
                int adjustedMax = AbilityFunctions.BladeWeaver.AdjustVolleyDamage(maxVolley, minVolley, maxVolley);
                int slashMin = ScaleBladeweaver(adjustedMin, mods.abilityDamagePct, dashRage);
                int slashMax = ScaleBladeweaver(adjustedMax, mods.abilityDamagePct, dashRage);
                int slashMinFull = ScaleBladeweaver(adjustedMin, mods.abilityDamagePct, maxRage);
                int slashMaxFull = ScaleBladeweaver(adjustedMax, mods.abilityDamagePct, maxRage);
                int firstMin = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMin, true);
                int firstMax = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMax, true);
                int firstMinFull = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMinFull, true);
                int firstMaxFull = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMaxFull, true);
                Pair(player, "First blade hit", FormatRangeNum(firstMin, firstMax), FormatRangeNum(firstMinFull, firstMaxFull), dashRage, maxRage);

                int repeatMin = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMin, false);
                int repeatMax = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMax, false);
                int repeatMinFull = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMinFull, false);
                int repeatMaxFull = AbilityFunctions.BladeWeaver.ScaleHitDamage(slashMaxFull, false);
                Pair(player, "Both blade hits", FormatRangeNum(firstMin + repeatMin, firstMax + repeatMax),
                    FormatRangeNum(firstMinFull + repeatMinFull, firstMaxFull + repeatMaxFull), dashRage, maxRage);

                float vsNow = BladeweaverWeaponVolleyMul(dashRage);
                float vsMin = minDmg > 0 ? vsNow * adjustedMin / minDmg : vsNow;
                float vsMax = maxDmg > 0 ? vsNow * adjustedMax / maxDmg : vsNow;
                Line(player, $"First hit vs sword: {FormatNumber(vsMin)}x min / {FormatNumber(vsMax)}x max at {dashRage} rage");
            }
            else
                Line(player, "Slash damage: equip a sword to see ability damage");
            Line(player, "Slash projectiles: 2");
            if (mods.projectileSizePct > 0)
                Line(player, $"Size: +{Pct(mods.projectileSizePct)}");

            float dashDist = DashDistance(dashRage, mods.abilityRangeBonus);
            float dashMax = DashDistance(maxRage, mods.abilityRangeBonus);
            Pair(player, "Dash distance", Tiles(dashDist), Tiles(dashMax), dashRage, maxRage);

            uint duration = AbilityFunctions.BladeWeaver.Dash_Duration + (uint)Math.Max(0, mods.durationBonusMs);
            float chargeMul = mods.chargeDurationMul > 0f ? mods.chargeDurationMul : 1f;
            float chargeSec = maxRage / AbilityFunctions.BladeWeaver.Rage_Charge_Per_Second * chargeMul;
            Line(player, $"Dash duration: {duration}ms | Charge to max: {FormatNumber(chargeSec)}s");
            if (mods.postDashInvulnMs > 0)
                Line(player, $"Invulnerable after dash: {FormatSec(mods.postDashInvulnMs)}");
            if (mods.rageKeep > 0)
                Line(player, $"Rage refund on slash hit: {Pct(mods.rageKeep)} of rage spent");
        }

        private static void WriteMinister(Player player, AbilityModifierSnapshot mods, int rage, int attack)
        {
            byte cost = AbilityFunctions.Minister.GetRageCost(rage);
            Line(player, $"Rage cost: {cost} (tiers 25 / 50 / 75 / 100)");

            int h25 = MinisterHeal(25, attack, mods);
            int h50 = MinisterHeal(50, attack, mods);
            int h75 = MinisterHeal(75, attack, mods);
            int h100 = MinisterHeal(100, attack, mods);
            Line(player, $"Heal/tick: {h25}/{h50}/{h75}/{h100} at 25/50/75/100 (this cast {MinisterHeal(cost, attack, mods)})");

            float radius = AbilityFunctions.Minister.GetPillarRadius(cost) + mods.abilityRadiusBonus;
            int durationMs = AbilityFunctions.Minister.GetPillarDurationMs(cost) + Math.Max(0, mods.durationBonusMs);
            int tickMs = Math.Max(400, mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : 2000);
            Line(player, $"Benefit radius: {Tiles(radius)} | Duration: {FormatSec(durationMs)} | Heal pulse every {FormatSec(tickMs)}");
            Line(player, "Pulses affect all players within the radius, including the caster");
            Line(player, "Field: +8 Vigor while within the benefit radius");
            Line(player, "Purify scales every 50 rage; Wrath and Aegis every 33 rage");
            int absorptionChunk = mods.absorptionRageChunk > 0 ? mods.absorptionRageChunk : SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
            int attackChunk = mods.timedAttackRageChunk > 0 ? mods.timedAttackRageChunk : SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
            int defenseChunk = mods.fieldDefenseRageChunk > 0 ? mods.fieldDefenseRageChunk : SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
            int absorption = SkillTreeFunctions.ScaleOnUseStat(mods.absorptionChance, cost, absorptionChunk);
            int wrath = SkillTreeFunctions.ScaleOnUseStat(mods.timedAttack, cost, attackChunk);
            int aegis = SkillTreeFunctions.ScaleOnUseStat(mods.fieldDefense, cost, defenseChunk);
            if (absorption > 0)
                Line(player, $"Purify on pulse: +{absorption}% Absorption Chance for {FormatSec(mods.absorptionChanceMs)}");
            if (wrath > 0)
                Line(player, $"Wrath on pulse: +{wrath} Attack for {FormatSec(mods.timedAttackMs)}");
            if (aegis > 0)
                Line(player, $"Aegis on pulse: +{aegis} Defense for {FormatSec(mods.fieldDefenseMs)}");
        }

        private static void WriteBrewer(Player player, AbilityModifierSnapshot mods, int rage)
        {
            WriteDumpRageCost(player, mods, rage);
            float rageScalar = rage / 100f;
            uint purpleMs = 1000 + (uint)(10000 * rageScalar) + (uint)Math.Max(0, mods.durationBonusMs);
            uint redMs = 1000 + (uint)(8000 * rageScalar) + (uint)Math.Max(0, mods.durationBonusMs);
            uint purpleFull = 1000 + 10000 + (uint)Math.Max(0, mods.durationBonusMs);
            uint redFull = 1000 + 8000 + (uint)Math.Max(0, mods.durationBonusMs);
            int rof = AbilityFunctions.Brewer.RoF_Amount + SkillTreeFunctions.ScaleOnUseStat(mods.rofAmount, rage);
            int vigor = 8 + SkillTreeFunctions.ScaleOnUseStat(mods.vigorBonus, rage);
            Pair(player, "Purple brew", $"+{rof}% RoF for {FormatSec((int)purpleMs)}",
                $"+{AbilityFunctions.Brewer.RoF_Amount + SkillTreeFunctions.ScaleOnUseStat(mods.rofAmount, 100)}% RoF for {FormatSec((int)purpleFull)}", rage);
            Pair(player, "Red brew", $"+{vigor} Vigor for {FormatSec((int)redMs)}",
                $"+{8 + SkillTreeFunctions.ScaleOnUseStat(mods.vigorBonus, 100)} Vigor for {FormatSec((int)redFull)}", rage);
            Line(player, $"Radius: {Tiles(AbilityFunctions.Brewer.GetAbilityArea(mods))}");
            if (mods.slowMs > 0)
                Line(player, $"Applies Slowed for {FormatSec(mods.slowMs)}");
        }

        /// <summary>
        /// Skill-tree on-use bonuses are stored per rage chunk and multiplied at cast time.
        /// Alchemist Blight is applied as a field buff in WriteAlchemist, so it is skipped here.
        /// </summary>
        private static void WriteOnUseStats(Player player, ClassType classType, AbilityModifierSnapshot mods, int rage)
        {
            // Ranger Blight is a flat cast bonus; other timed Attack bonuses use rage chunks.
            if (classType == ClassType.Ranger && mods.timedAttack > 0 && mods.timedAttackMs > 0)
                Line(player, $"On use: +{mods.timedAttack} Attack for {FormatSec(mods.timedAttackMs)}");
            else if (classType != ClassType.Alchemist && classType != ClassType.Minister)
                OnUseStat(player, mods.timedAttack, mods.timedAttackMs, rage, "Attack");
            OnUseStat(player, mods.hymnDefense, mods.hymnDefenseMs, rage, "Defense");
            int healthChunk = mods.hymnMaxHealthRageChunk > 0 ? mods.hymnMaxHealthRageChunk : SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
            OnUseStat(player, mods.hymnMaxHealth, mods.hymnMaxHealthMs, rage, "Max Health", healthChunk);
            OnUseStat(player, mods.hymnBlockChance, mods.hymnBlockChanceMs, rage, "Block Chance", SkillTreeFunctions.On_Use_Stat_Rage_Chunk, true);
            if (classType != ClassType.Alchemist && classType != ClassType.Berserker && classType != ClassType.Brewer && classType != ClassType.Minister)
                OnUseStat(player, mods.fieldDefense, mods.fieldDefenseMs, rage, "Defense");
            int absorbChunk = mods.absorptionRageChunk > 0 ? mods.absorptionRageChunk : SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
            if (classType != ClassType.Minister)
                OnUseStat(player, mods.absorptionChance, mods.absorptionChanceMs, rage, "Absorption Chance", absorbChunk, true);
            if (classType == ClassType.Bladeweaver && mods.speedOnHit > 0 && mods.speedOnHitMs > 0)
            {
                int chunk = SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
                int now = SkillTreeFunctions.ScaleOnUseStat(mods.speedOnHit, rage, chunk);
                Line(player, $"On slash hit: +{now} Speed for {FormatSec(mods.speedOnHitMs)} ({mods.speedOnHit} per {chunk} rage consumed)");
            }
            if (classType == ClassType.Brewer && mods.hymnDefense > 0 && mods.timedDefenseMs > 0)
                OnUseStat(player, mods.hymnDefense, mods.timedDefenseMs, rage, "Defense");
        }

        private static void WriteTalismanOnUse(Player player, AbilityModifierSnapshot mods, int rage)
        {
            if (mods.talismanEffects == null) return;
            for (int i = 0; i < mods.talismanEffects.Length; i++)
            {
                var effect = mods.talismanEffects[i];
                if (effect.trigger != TalismanTrigger.AbilityUse) continue;
                int need = TalismanEffect.GetRequiredRageThreshold(effect);
                string ready = rage >= need ? "" : $" (needs {need}% rage)";
                Line(player, $"Talisman on use: {DescribeTalismanChat(effect)}{ready}");
            }
        }

        private static void WriteDumpRageCost(Player player, AbilityModifierSnapshot mods, int rage)
        {
            byte spent = DumpSpent(mods, rage);
            if (mods.rageKeep > 0)
                Line(player, $"Rage cost: dump {spent} (keep {Pct(mods.rageKeep)})");
            else
                Line(player, $"Rage cost: dump all ({spent})");
        }

        private static byte DumpSpent(AbilityModifierSnapshot mods, int rage)
        {
            byte integral = (byte)Math.Max(0, Math.Min(100, rage));
            AbilityFunctions.RageSpend.SpendDumpRage(ref integral, mods, out var cost);
            return cost;
        }

        private static void OnUseStat(Player player, int perChunk, int durationMs, int rage, string stat, int chunk = 0, bool percent = false)
        {
            if (perChunk <= 0 || durationMs <= 0) return;
            if (chunk < 1)
                chunk = SkillTreeFunctions.On_Use_Stat_Rage_Chunk;
            int now = SkillTreeFunctions.ScaleOnUseStat(perChunk, rage, chunk);
            string unit = percent ? "%" : "";
            Line(player, $"On use: +{now}{unit} {stat} for {FormatSec(durationMs)} ({perChunk}{unit} per {chunk} rage consumed)");
        }

        private static bool TryGetWeaponRaw(PlayerState playerState, out int minDamage, out int maxDamage)
        {
            minDamage = 0;
            maxDamage = 0;
            var item = GetHeldWeapon(playerState, out var weapon);
            if (weapon == null) return false;
            WeaponFunctions.GetProjectileDamage(weapon.slotType, weapon.projectiles[0], out var min, out var max);
            minDamage = min;
            maxDamage = max;
            return true;
        }

        private static bool TryGetWeaponOutgoing(PlayerState playerState, uint time, out float minDamage, out float maxDamage)
        {
            minDamage = 0;
            maxDamage = 0;
            var item = GetHeldWeapon(playerState, out var weapon);
            if (weapon == null) return false;

            WeaponFunctions.GetProjectileDamage(weapon.slotType, weapon.projectiles[0], out var min, out var max);
            float enchantMod = item.enchantType == ItemEnchantType.Damaging ? EnchantFunctions.Damage(item.enchantLevel) : 1f;
            bool damaging = playerState.HasEffect(StatusEffect.Damaging, time);
            float attackMod = StatFunctions.AttackModifier(playerState.currentSnapshot.GetFunctionalStat(StatType.Attack), damaging);
            minDamage = min * attackMod * enchantMod;
            maxDamage = max * attackMod * enchantMod;
            return true;
        }

        private static bool TryGetWeaponVolleyOutgoing(PlayerState playerState, uint time, out float minDamage, out float maxDamage)
        {
            minDamage = 0;
            maxDamage = 0;
            var item = GetHeldWeapon(playerState, out var weapon);
            if (weapon == null) return false;

            WeaponFunctions.GetVolleyDamage(weapon.slotType, weapon.projectiles, out var min, out var max);
            float enchantMod = item.enchantType == ItemEnchantType.Damaging ? EnchantFunctions.Damage(item.enchantLevel) : 1f;
            bool damaging = playerState.HasEffect(StatusEffect.Damaging, time);
            float attackMod = StatFunctions.AttackModifier(playerState.currentSnapshot.GetFunctionalStat(StatType.Attack), damaging);
            minDamage = min * attackMod * enchantMod;
            maxDamage = max * attackMod * enchantMod;
            return true;
        }

        private static Item GetHeldWeapon(PlayerState playerState, out WeaponInfo weapon)
        {
            weapon = null;
            var equips = playerState.currentSnapshot.equips;
            if (equips == null || equips.Length == 0) return Item.Blank;
            var item = equips[0];
            if (item.IsBlank || !(item.GetInfo() is WeaponInfo info) || info.projectiles == null || info.projectiles.Length == 0)
                return Item.Blank;
            weapon = info;
            return item;
        }

        private static int ScaleDamage(int baseDamage, float abilityDamagePct)
        {
            return Math.Max(1, (int)(baseDamage * (1f + abilityDamagePct)));
        }

        private static int ScaleLancer(float weaponOutgoing, float abilityDamagePct, int rage)
        {
            int scaled = AbilityFunctions.Lancer.ScaleWeaponDamage((int)Math.Round(weaponOutgoing));
            scaled = AbilityFunctions.RageSpend.ApplyRageDamageMul(scaled, rage);
            return Math.Max(1, (int)(scaled * (1f + abilityDamagePct)));
        }

        private static int ScaleBerserker(float weaponOutgoing, float abilityDamagePct, int rage)
        {
            int scaled = AbilityFunctions.Berserker.ScaleWeaponDamage((int)Math.Round(weaponOutgoing));
            scaled = AbilityFunctions.RageSpend.ApplyRageDamageMul(scaled, rage, AbilityFunctions.Berserker.Rage_Damage_At_100);
            return Math.Max(1, (int)(scaled * (1f + abilityDamagePct)));
        }

        private static int ScaleAlchemist(float weaponOutgoing, float abilityDamagePct, int rage)
        {
            int scaled = AbilityFunctions.Alchemist.ScaleWeaponDamage((int)Math.Round(weaponOutgoing));
            scaled = AbilityFunctions.RageSpend.ApplyRageDamageMul(scaled, rage, AbilityFunctions.Alchemist.Rage_Damage_At_100);
            return Math.Max(1, (int)(scaled * (1f + abilityDamagePct)));
        }

        private static int ScaleRanger(float weaponOutgoing, float abilityDamagePct, int rage, int attack, bool damaging)
        {
            int scaled = AbilityFunctions.Ranger.ScaleWeaponDamage((int)Math.Round(weaponOutgoing), attack, damaging);
            scaled = AbilityFunctions.RageSpend.ApplyRageDamageMul(scaled, rage, AbilityFunctions.Ranger.Rage_Damage_At_100);
            return Math.Max(1, (int)(scaled * (1f + abilityDamagePct)));
        }

        private static int ScaleBladeweaver(float weaponOutgoing, float abilityDamagePct, int rage)
        {
            int scaled = AbilityFunctions.BladeWeaver.ScaleWeaponDamage((int)Math.Round(weaponOutgoing), rage);
            return Math.Max(1, (int)(scaled * (1f + abilityDamagePct)));
        }

        private static float LancerWeaponShotMul(int rage)
        {
            return AbilityFunctions.Lancer.Weapon_Damage_Mul
                * AbilityFunctions.RageSpend.Damage_Mul_At_100_Rage
                * Math.Max(0, rage) / 100f;
        }

        private static float BerserkerWeaponShotMul(int rage)
        {
            return AbilityFunctions.Berserker.Weapon_Damage_Mul
                * AbilityFunctions.Berserker.Rage_Damage_At_100
                * Math.Max(0, rage) / 100f;
        }

        private static float AlchemistWeaponShotMul(int rage)
        {
            return AbilityFunctions.Alchemist.Weapon_Damage_Mul
                * AbilityFunctions.Alchemist.Rage_Damage_At_100
                * Math.Max(0, rage) / 100f;
        }

        private static float RangerVsDisplayedVolley(int rage, int attack, bool damaging)
        {
            float full = StatFunctions.AttackModifier(attack, damaging);
            if (full < 0.01f)
                full = 0.01f;
            float partial = AbilityFunctions.Ranger.PartialAttackModifier(attack, damaging);
            return (partial / full)
                * AbilityFunctions.Ranger.Weapon_Damage_Mul
                * AbilityFunctions.Ranger.Rage_Damage_At_100
                * Math.Max(0, rage) / 100f;
        }

        private static float BladeweaverWeaponVolleyMul(int rage)
        {
            return AbilityFunctions.BladeWeaver.Weapon_Damage_Mul * AbilityFunctions.BladeWeaver.First_Hit_Damage_Mul
                * Math.Min(1f, Math.Max(0, rage) / (float)AbilityFunctions.BladeWeaver.Max_Dash_Rage);
        }

        private static int MinisterHeal(int rage, int attack, AbilityModifierSnapshot mods)
        {
            int heal = AbilityFunctions.Minister.GetHealAmount(rage, attack);
            return Math.Max(0, (int)(heal * (1f + mods.healPower)));
        }

        private static float DashDistance(int rage, float extraDistance)
        {
            float rageScalar = Math.Min(rage / (float)AbilityFunctions.BladeWeaver.Max_Dash_Rage, 1f);
            return rageScalar * (6f + extraDistance);
        }

        private static float ProjectileRange(ProjectileData shot)
        {
            if (shot is AoeProjectileData aoeData)
                return aoeData.range;
            return (int)Math.Round(shot.lifetime * shot.speed * 100) / 100f;
        }

        private static string DescribeTalismanChat(TalismanEffect effect)
        {
            if (effect.statBonus != null && effect.statBonus.amount != 0)
                return $"+{effect.statBonus.amount} {ProcFunctions.GetStatDisplayName(effect.statBonus.statType)} for {FormatSec((int)effect.statBonus.durationMs)}";
            if (effect.alternateStatBonus != null && effect.alternateStatBonus.amount != 0)
                return $"{ProcFunctions.FormatAlternateStatAmount(effect.alternateStatBonus.statType, effect.alternateStatBonus.amount)} {ProcFunctions.GetAlternateStatDisplayName(effect.alternateStatBonus.statType)} for {FormatSec((int)effect.alternateStatBonus.durationMs)}";
            if (effect.healAmount != 0)
                return $"heal {effect.healAmount} HP";
            if (effect.rofAmount != 0)
                return $"+{effect.rofAmount}% Rate of Fire";
            if (effect.aoe != null && effect.aoe.damage > 0)
                return $"{FormatNumber(effect.aoe.radius)} tile AoE for {effect.aoe.damage} damage";
            if (Math.Abs(effect.rageGain) > 0.001f)
                return $"+{FormatNumber(effect.rageGain)} rage";
            return TalismanEffect.GetTriggerDisplayName(effect.trigger);
        }

        private static void Pair(Player player, string label, string atRage, string atFull, int rage, int fullRage = 100)
        {
            if (rage >= fullRage || atRage == atFull)
                Line(player, $"{label}: {atRage}");
            else
                Line(player, $"{label}: {atRage} ({atFull} at {fullRage} rage)");
        }

        private static void Line(Player player, string text)
        {
            if (text.Length > NetConstants.Max_Chat_Length)
                text = text.Substring(0, NetConstants.Max_Chat_Length);
            player.AddChat(ChatData.Info(text));
        }

        private static string Num(int value) => value.ToString();

        private static string Tiles(float value) => $"{FormatNumber(value)} tiles";

        private static string Pct(float value) => $"{FormatNumber(value * 100)}%";

        private static string FormatRangeNum(int min, int max)
        {
            if (min == max) return min.ToString();
            return $"{min}-{max}";
        }

        private static string FormatSec(int ms)
        {
            if (ms <= 0) return "0s";
            return $"{FormatNumber(ms / 1000f)}s";
        }

        private static string FormatNumber(float value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.05f)
                return ((int)Math.Round(value)).ToString();
            return value.ToString("0.#");
        }
    }
}
