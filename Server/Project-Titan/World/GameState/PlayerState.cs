using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Components;
using TitanCore.Data.Entities;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Models;
using TitanCore.Net.Packets.Server;
using Utils.NET.Geometry;
using Utils.NET.Logging;
using World.Abilities;
using World.Map.Objects.Abilities;
using World.Map.Objects.Entities;
using World.Map.Objects.Map;
using World.Net;

namespace World.GameState
{
    public struct PlayerSnapshot
    {
        public static PlayerSnapshot GetDefault()
        {
            return new PlayerSnapshot()
            {
                maxHealth = 100,
                attack = 0,
                defense = 0,
                vigor = 0,
                radius = 0.3f,
                fullSouls = 0,
                equips = new Item[4],
                extraStats = new Dictionary<StatType, int>(),
                extraAlternateStats = new Dictionary<AlternateStatType, int>()
            };
        }

        private int maxHealth;

        private int speed;

        private int attack;

        private int defense;

        private int vigor;

        public int maxHealthBonus;

        public int speedBonus;

        public int attackBonus;

        public int defenseBonus;

        public int vigorBonus;

        public float radius;

        public int fullSouls;

        public uint target;

        public int heal;

        public int health;

        public int serverDamage;

        public uint serverEffects;

        public Item[] equips;

        public Dictionary<StatType, int> extraStats;

        public Dictionary<AlternateStatType, int> extraAlternateStats;

        public uint time;

        public PlayerSnapshot(NetStat[] stats, PlayerSnapshot previous, uint time)
        {
            this.time = time;
            maxHealth = previous.maxHealth;
            speed = previous.speed;
            attack = previous.attack;
            defense = previous.defense;
            vigor = previous.vigor;
            maxHealthBonus = previous.maxHealthBonus;
            speedBonus = previous.speedBonus;
            attackBonus = previous.attackBonus;
            defenseBonus = previous.defenseBonus;
            vigorBonus = previous.vigorBonus;
            radius = previous.radius;
            fullSouls = previous.fullSouls;
            serverEffects = previous.serverEffects;
            equips = new Item[previous.equips.Length];
            for (int i = 0; i < equips.Length; i++)
                equips[i] = previous.equips[i];

            extraStats = new Dictionary<StatType, int>(previous.extraStats);
            extraAlternateStats = new Dictionary<AlternateStatType, int>(previous.extraAlternateStats);
            target = previous.target;
            health = previous.health;
            heal = 0;
            serverDamage = 0;

            foreach (var stat in stats)
            {
                switch (stat.type)
                {
                    case ObjectStatType.Health:
                        health = (int)stat.value;
                        break;
                    case ObjectStatType.MaxHealth:
                        maxHealth = (int)stat.value;
                        break;
                    case ObjectStatType.Speed:
                        speed = (int)stat.value;
                        break;
                    case ObjectStatType.Attack:
                        attack = (int)stat.value;
                        break;
                    case ObjectStatType.Defense:
                        defense = (int)stat.value;
                        break;
                    case ObjectStatType.Vigor:
                        vigor = (int)stat.value;
                        break;
                    case ObjectStatType.MaxHealthBonus:
                        maxHealthBonus = (int)stat.value;
                        break;
                    case ObjectStatType.SpeedBonus:
                        speedBonus = (int)stat.value;
                        break;
                    case ObjectStatType.AttackBonus:
                        attackBonus = (int)stat.value;
                        break;
                    case ObjectStatType.DefenseBonus:
                        defenseBonus = (int)stat.value;
                        break;
                    case ObjectStatType.VigorBonus:
                        vigorBonus = (int)stat.value;
                        break;
                    case ObjectStatType.Souls:
                        fullSouls = (int)stat.value;
                        break;
                    case ObjectStatType.StatusEffects:
                        serverEffects = (uint)stat.value;
                        break;
                    case ObjectStatType.Target:
                        target = (uint)stat.value;
                        break;
                    case ObjectStatType.Heal:
                        heal = (int)stat.value;
                        break;
                    case ObjectStatType.ServerDamage:
                        serverDamage = (int)stat.value;
                        break;
                    case ObjectStatType.Inventory0:
                    case ObjectStatType.Inventory1:
                    case ObjectStatType.Inventory2:
                    case ObjectStatType.Inventory3:
                        var item = (Item)stat.value;
                        int index = (int)stat.type - (int)ObjectStatType.Inventory0;
                        equips[index] = item;
                        EquipmentStatFunctions.RecalculateEquipmentStats(equips, extraStats, extraAlternateStats);
                        break;
                }
            }
        }

        public int GetBaseStat(StatType type)
        {
            switch (type)
            {
                case StatType.MaxHealth:
                    return maxHealth;
                case StatType.Speed:
                    return speed;
                case StatType.Attack:
                    return attack;
                case StatType.Defense:
                    return defense;
                case StatType.Vigor:
                    return vigor;
            }
            return 0;
        }

        public int GetFunctionalStat(StatType type)
        {
            if (!extraStats.TryGetValue(type, out var amount))
                amount = 0;
            switch (type)
            {
                case StatType.MaxHealth:
                    return StatFunctions.ClampPlayerMaxHealth(maxHealth + maxHealthBonus + amount);
                case StatType.Speed:
                    return speed + speedBonus + amount;
                case StatType.Attack:
                    return attack + attackBonus + amount;
                case StatType.Defense:
                    return defense + defenseBonus + amount;
                case StatType.Vigor:
                    return vigor + vigorBonus + amount;
            }
            return amount;
        }

        public int GetAlternateStat(AlternateStatType type)
        {
            if (!extraAlternateStats.TryGetValue(type, out var amount))
                amount = 0;
            return amount;
        }

        public int GetEquippedAlternateStat(AlternateStatType type)
        {
            return ItemFunctions.GetEquippedAlternateStat(equips, type);
        }

        public bool HasServerEffect(StatusEffect effect)
        {
            return ((serverEffects >> (int)effect) & 1) == 1;
        }
    }

    public class PlayerState
    {
        /// <summary>
        /// The currrent snapshot
        /// </summary>
        public PlayerSnapshot currentSnapshot;

        /// <summary>
        /// The health of the player
        /// </summary>
        private float health;

        /// <summary>
        /// The last time the health was advanced
        /// </summary>
        private uint lastHealthTime;

        /// <summary>
        /// The amount of rage the player has
        /// </summary>
        public float rage;

        /// <summary>
        /// Rage percent when the current ability was activated.
        /// </summary>
        public float abilityActivationRage;

        /// <summary>
        /// The next time an ability is available to use
        /// </summary>
        public uint nextAbility;

        /// <summary>
        /// Status effects that were applied by the client
        /// </summary>
        private Dictionary<StatusEffect, StatusEffectTime> clientEffects = new Dictionary<StatusEffect, StatusEffectTime>();

        private Player player;

        private CharacterInfo classInfo;

        public uint positionalEffectStartTime;

        public Vec2 positionalEffectPosition;

        public Vec2 positionalEffectVector;

        public bool positionalEffectCollided = false;

        private uint currentMoveCheckTime = 0;

        private Vec2 currentMoveCheckPosition;

        private bool wasSlowed = false;

        private bool wasWasSlowed = false;

        private bool wasSpeedy = false;

        private bool wasWasSpeedy = false;

        public ClassAbility ability;

        public AbilityModifierSnapshot abilityMods;

        private readonly Dictionary<int, uint> procCooldowns = new Dictionary<int, uint>();

        private readonly Dictionary<int, uint> talismanCooldowns = new Dictionary<int, uint>();

        private readonly Dictionary<long, uint> talismanShotRage = new Dictionary<long, uint>();

        private readonly Dictionary<uint, (uint burst, int count)> lancerNovaHits = new Dictionary<uint, (uint, int)>();

        private readonly HashSet<int> triggeredProcKeys = new HashSet<int>();

        private readonly List<TimedStatBonus> timedStatBonuses = new List<TimedStatBonus>();

        private readonly List<TimedAlternateStatBonus> timedAlternateStatBonuses = new List<TimedAlternateStatBonus>();

        private struct TimedStatSource : IEquatable<TimedStatSource>
        {
            public byte kind;
            public int id;

            public static TimedStatSource ByStatType => new TimedStatSource { kind = 0 };

            public static TimedStatSource Talisman(int effectIndex) => new TimedStatSource { kind = 1, id = effectIndex };

            public static TimedStatSource MinisterField(StatType statType) => new TimedStatSource { kind = 2, id = (int)statType };

            public static TimedStatSource CommanderField => new TimedStatSource { kind = 3 };

            public static TimedStatSource CommanderPulse => new TimedStatSource { kind = 4 };

            public bool Equals(TimedStatSource other) => kind == other.kind && id == other.id;

            public override bool Equals(object obj) => obj is TimedStatSource other && Equals(other);

            public override int GetHashCode() => (kind * 397) ^ id;
        }

        private struct TimedStatBonus
        {
            public TimedStatSource source;
            public StatType statType;
            public StatusEffect effect;
            public int amount;
            public uint endTime;
            public bool hasEffect;
        }

        private struct TimedAlternateStatBonus
        {
            public AlternateStatType statType;
            public StatusEffect effect;
            public int amount;
            public uint endTime;
            public bool hasEffect;
        }

        public uint LastClientTime { get; private set; }

        public PlayerState(uint time, Player player, NewObjectStats newObj)
        {
            this.player = player;
            LastClientTime = time;
            currentSnapshot = new PlayerSnapshot(newObj.stats, PlayerSnapshot.GetDefault(), time);
            health = currentSnapshot.health;
            lastHealthTime = time;
            classInfo = (CharacterInfo)GameData.objects[newObj.type];
            currentMoveCheckTime = time;
            currentMoveCheckPosition = player.position.Value;
            GetTargetTps(time);

            ability = ClassAbility.GetAbility((ClassType)player.info.id);
            ability.SetPlayer(player);
            abilityMods = player.BuildAbilityModifiers();
        }

        public void StartHealth(uint time)
        {
            lastHealthTime = time;
        }

        public void PushUpd(uint time, UpdatedObjectStats updStats)
        {
            LastClientTime = time;
            AdvanceHealth(time - Client.Client_Fixed_Delta);
            currentSnapshot = new PlayerSnapshot(updStats.stats, currentSnapshot, time);
            wasSlowed = HasClientEffect(StatusEffect.Slowed, time);
            wasSpeedy = HasClientEffect(StatusEffect.Speedy, time);
            health = Math.Min(health + currentSnapshot.heal - currentSnapshot.serverDamage, currentSnapshot.GetFunctionalStat(StatType.MaxHealth));
            if (health <= 0)
            {
                Die(player.lastServerDamager);
            }

            currentSnapshot.heal = 0;
            currentSnapshot.serverDamage = 0;
            AdvanceHealth(time);
            ability?.Tick(time);
        }

        private float GetTargetTps(uint time)
        {
            var slowed = HasEffect(StatusEffect.Slowed, time);
            var speedy = HasEffect(StatusEffect.Speedy, time);

            var speed = Math.Max(player.GetStatFunctional(StatType.Speed), currentSnapshot.GetFunctionalStat(StatType.Speed));
            var tps = StatFunctions.TilesPerSecond(speed,
                wasWasSlowed && wasSlowed && slowed,
                wasWasSpeedy || wasSpeedy || speedy);

            wasWasSlowed = slowed;
            wasWasSpeedy = speedy;

            return tps;
        }

        public void DidGoto(Vec2 position, uint time)
        {
            currentMoveCheckTime = time;
            currentMoveCheckPosition = position;
            GetTargetTps(time);
        }

        public bool AdvancePosition(Vec2 position, uint time)
        {
            if (time == currentMoveCheckTime)
            {
                return position.DistanceTo(currentMoveCheckPosition) < 1;
            }

            if (!HasEffect(StatusEffect.Charmed, currentMoveCheckTime) && !HasEffect(StatusEffect.Charmed, time))
            {
                if (TryGetStatusEffectPosition(currentMoveCheckTime, time, out var effectPosition, out currentMoveCheckTime))
                {
                    currentMoveCheckPosition = effectPosition;
                    if (currentMoveCheckTime == time)
                    {
                        return position.DistanceTo(effectPosition) < 2;
                    }
                }

                var timeDif = time - currentMoveCheckTime;
                if (timeDif > 2000)
                {
                    currentMoveCheckPosition = position;
                    currentMoveCheckTime = time;
                    return true;
                }

                var tps = GetTargetTps(time);
                var elapsed = timeDif / 1000f;
                var distance = currentMoveCheckPosition.DistanceTo(position);
                var maxDistance = tps * (elapsed + NetConstants.Client_Delta / 1000f) + 0.15f;
                if (distance > maxDistance)
                {
                    player.client.SendAsync(new TnError("Movement check failed! Moving too fast."));
                    //player.client.Disconnect();
                    return false;
                }
            }

            currentMoveCheckPosition = position;
            currentMoveCheckTime = time;
            return true;
        }

        public float Health(uint time)
        {
            AdvanceHealth(time);
            return health;
        }

        public void AdvanceTime(uint time)
        {
            AdvanceHealth(time);
        }

        public void Damage(uint time, int damage, GameObjectInfo damagerInfo, uint damagerId, uint projectileId)
        {
            AdvanceHealth(time);
            var seed = StatFunctions.GetCombatSeed(projectileId, time, player.gameId);
            var result = StatFunctions.ResolveIncomingDamage(
                damage,
                currentSnapshot.equips,
                0,
                GetTimedAlternateStatBonus(AlternateStatType.BlockChance),
                GetTimedAlternateStatBonus(AlternateStatType.AbsorptionChance),
                currentSnapshot.GetFunctionalStat(StatType.Defense),
                HasEffect(StatusEffect.Fortified, time),
                seed);
            var damageTaken = result.damage;
            var previousHp = health;
            health -= damageTaken;
            if (health > currentSnapshot.GetFunctionalStat(StatType.MaxHealth))
                health = currentSnapshot.GetFunctionalStat(StatType.MaxHealth);
            player.hitDamage.Value += damageTaken;

            TriggerProcsFromDamageResult(result, time);

            if (health <= 0 && previousHp > 0)
            {
                Die(damagerInfo);
                if (damagerId != 0)
                {
                    if (player.world.objects.TryGetEnemy(damagerId, out var damager))
                        damager.emote.Value = EmoteType.F;
                }
            }
        }

        public void Die(GameObjectInfo damagerInfo)
        {
            player.Die(damagerInfo);
        }

        public int GetDamageTaken(int damage, uint time)
        {
            var defense = currentSnapshot.GetFunctionalStat(StatType.Defense);
            return StatFunctions.DamageTaken(defense, damage, HasEffect(StatusEffect.Fortified, time), HasEffect(StatusEffect.DefenseMinus, time) ? player.GetDefenseMinusAmount() : 0);
        }

        public int GetTimedAlternateStatBonus(AlternateStatType type)
        {
            return GetTimedAlternateStatBonus(type, uint.MaxValue);
        }

        public int GetTimedAlternateStatBonus(AlternateStatType type, uint time)
        {
            int total = 0;
            for (int i = 0; i < timedAlternateStatBonuses.Count; i++)
            {
                var bonus = timedAlternateStatBonuses[i];
                if (bonus.statType != type) continue;
                if (time != uint.MaxValue && time >= bonus.endTime) continue;
                total += bonus.amount;
            }
            return total;
        }

        public Dictionary<AlternateStatType, int> GetTimedAlternateStatBonusesForScaling(uint time = uint.MaxValue)
        {
            var bonuses = new Dictionary<AlternateStatType, int>();
            for (int i = 0; i < timedAlternateStatBonuses.Count; i++)
            {
                var bonus = timedAlternateStatBonuses[i];
                if (time != uint.MaxValue && time >= bonus.endTime) continue;

                if (!bonuses.TryGetValue(bonus.statType, out var amount))
                    amount = 0;
                if (bonus.statType == AlternateStatType.RateOfFire)
                    bonuses[bonus.statType] = Math.Max(amount, bonus.amount);
                else
                    bonuses[bonus.statType] = amount + bonus.amount;
            }
            return bonuses;
        }

        public DamageResult ResolvePlayerOutgoingDamage(
            int rawDamage,
            int defenderBlockChance,
            int defenderAbsorptionChance,
            int defenderDefense,
            bool defenderFortified,
            uint projectileId,
            uint time,
            uint targetId,
            int defenderDefenseMinusAmount = 0)
        {
            return StatFunctions.ResolveOutgoingDamage(
                rawDamage,
                currentSnapshot.equips,
                GetTimedAlternateStatBonus(AlternateStatType.TrueDamageChance),
                GetTimedAlternateStatBonus(AlternateStatType.CriticalStrikeChance),
                GetTimedAlternateStatBonus(AlternateStatType.CriticalStrikeDamage),
                defenderBlockChance,
                defenderAbsorptionChance,
                defenderDefense,
                defenderFortified,
                projectileId,
                time,
                targetId,
                player.gameId,
                defenderDefenseMinusAmount);
        }

        private void AdvanceHealth(uint time)
        {
            AdvanceTimedBonuses(time);
            while (lastHealthTime < time)
            {
                lastHealthTime += NetConstants.Client_Delta;
                var regen = StatFunctions.HealthRegen(currentSnapshot.GetFunctionalStat(StatType.Vigor), NetConstants.Client_Delta, HasEffect(StatusEffect.Healing, lastHealthTime), HasEffect(StatusEffect.Sick, lastHealthTime));
                health += regen;

                if (health > currentSnapshot.GetFunctionalStat(StatType.MaxHealth))
                    health = currentSnapshot.GetFunctionalStat(StatType.MaxHealth);

                //Log.Write(lastHealthTime / NetConstants.Client_Delta);
                //Log.Write(health, ConsoleColor.Green);
            }
        }

        public void UseAbility(uint time, Vec2 position, Vec2 target, byte value)
        {
            if (time < nextAbility)
                return;

            if (HasPositionalEffect(time))
            {
                //Log.Write("Ability used during a positional effect!");
                player.client.SendAsync(new TnError("Ability used during a positional effect!"));
                return;
            }

            if (!HasEnoughRageForAbility())
            {
                SyncRageToClient();
                return;
            }

            int attack = currentSnapshot.GetFunctionalStat(StatType.Attack);

            var rageBefore = rage;
            var rageIntegral = (byte)Math.Min(Math.Floor(rageBefore), 100);
            abilityActivationRage = rageIntegral;
            var rageByte = rageIntegral;
            var worldEffectPacket = ability.UseAbility(time, position, target, value, attack, ref rageByte, out var rageCost, out var sendToSelf, out var failed);

            if (failed)
            {
                SyncRageToClient();
                player.client.SendAsync(new TnError("Failed to use ability"));
                return;
            }

            rage = StatFunctions.ApplyAbilityRageSpend(rageBefore, rageIntegral, rageByte);
            SyncRageToClient();

            //Log.Write($"Rage: {rage}");

            var effects = AbilityFunctions.GetAbilityEffects(rageCost, attack, value, (ClassType)classInfo.id);

            for (int i = 0; i < effects.Count; i++) // add to client
            {
                var effect = effects[i];
                //Log.Write($"Added Effect: {effect.type}, Duration: {effect.duration}");
                AddClientStatusEffect(effect.type, time, effect.duration);
            }

            foreach (var otherPlayer in player.playersSentTo)
            {
                if (!sendToSelf && otherPlayer == player) continue;
                for (int i = 0; i < effects.Count; i++) // add to client
                {
                    var effect = effects[i];
                    if (otherPlayer.DistanceTo(position) < effect.area)
                        otherPlayer.AddEffect(effect.type, effect.duration / 1000f);
                }

                if (worldEffectPacket != null)
                    otherPlayer.client.SendAsync(worldEffectPacket);
            }

            if (SkillTreeFunctions.IsEnabled)
                ability.TriggerTalisman(TalismanTrigger.AbilityUse, time, position, target);

            nextAbility = time + (uint)GetAbilityCooldownMs(rageCost);
        }

        public bool IsInvincible(uint time)
        {
            return HasClientEffect(StatusEffect.Dashing, time) || HasEffect(StatusEffect.Invincible, time) || HasClientEffect(StatusEffect.KnockedBack, time) || HasClientEffect(StatusEffect.Grounded, time);
        }

        private bool HasPositionalEffect(uint time)
        {
            return HasClientEffect(StatusEffect.Charmed, time) || HasClientEffect(StatusEffect.Dashing, time) || HasClientEffect(StatusEffect.KnockedBack, time) || HasClientEffect(StatusEffect.Grounded, time);
        }

        public bool TryGetStatusEffectPosition(uint fromTime, uint toTime, out Vec2 position, out uint afterTime)
        {
            for (uint time = fromTime; time < toTime; time += NetConstants.Client_Delta)
            {
                if (HasClientEffect(StatusEffect.Charmed, time))
                {
                    position = GetPositionalEffectPosition(fromTime, toTime, StatusEffect.Charmed, out afterTime);
                    return true;
                }
                if (HasClientEffect(StatusEffect.Dashing, time))
                {
                    position = GetPositionalEffectPosition(fromTime, toTime, StatusEffect.Dashing, out afterTime);
                    return true;
                }
                if (HasClientEffect(StatusEffect.KnockedBack, time))
                {
                    position = GetPositionalEffectPosition(fromTime, toTime, StatusEffect.KnockedBack, out afterTime);
                    return true;
                }
                if (HasClientEffect(StatusEffect.Grounded, time))
                {
                    position = GetPositionalEffectPosition(fromTime, toTime, StatusEffect.Grounded, out afterTime);
                    return true;
                }
            }

            afterTime = fromTime;
            position = Vec2.zero;
            return false;
        }

        private Vec2 GetPositionalEffectPosition(uint fromTime, uint toTime, StatusEffect effect, out uint time)
        {
            for (time = Math.Max(fromTime, positionalEffectStartTime); time < toTime; time += NetConstants.Client_Delta)
            {
                if (!HasClientEffect(effect, time)) return positionalEffectPosition;

                if (!positionalEffectCollided)
                {
                    var newPos = positionalEffectPosition + positionalEffectVector;
                    if (!PlayerCanWalk(newPos))
                        positionalEffectCollided = true;
                    else
                        positionalEffectPosition = newPos;
                }
            }
            return positionalEffectPosition;
        }

        private bool PlayerCanWalk(Vec2 position)
        {
            return player.world.tiles.PlayerCanWalk(position.x, position.y);
        }

        public void AddCharmed(Vec2 position, Vec2 charmerPosition, uint time, uint duration)
        {
            if (HasPositionalEffect(time)) return;
            positionalEffectCollided = false;
            positionalEffectPosition = position;
            positionalEffectStartTime = time + NetConstants.Client_Delta;
            positionalEffectVector = StatusEffectFunctions.GetCharmedPositionVector(position, charmerPosition) * (NetConstants.Client_Delta / 1000f);
            AddClientStatusEffect(StatusEffect.Charmed, time, duration);
        }

        public void AddDashing(Vec2 position, Vec2 target, uint time, int rage, uint durationMs, float extraDistance)
        {
            if (HasPositionalEffect(time)) return;
            positionalEffectCollided = false;
            positionalEffectPosition = position;
            positionalEffectStartTime = time + NetConstants.Client_Delta;
            positionalEffectVector = AbilityFunctions.BladeWeaver.GetDashPositionVector(position.AngleTo(target), rage, durationMs, extraDistance) * (NetConstants.Client_Delta / 1000f);
            AddClientStatusEffect(StatusEffect.Dashing, time, durationMs);
            AddClientStatusEffect(StatusEffect.Invulnerable, time, durationMs);
        }

        public void AddDashing(Vec2 position, Vec2 target, uint time, int rage, uint durationMs)
        {
            AddDashing(position, target, time, rage, durationMs, 0);
        }

        public void AddDashing(Vec2 position, Vec2 target, uint time, int rage)
        {
            AddDashing(position, target, time, rage, AbilityFunctions.BladeWeaver.Dash_Duration, 0);
        }

        public void AddKnockedBack(Vec2 position, Vec2 knockerPosition, uint time, uint duration)
        {
            var knockbackResistance = currentSnapshot.GetAlternateStat(AlternateStatType.KnockbackResistance);
            if (knockbackResistance >= 100) return;
            if (HasPositionalEffect(time)) return;
            positionalEffectCollided = false;
            positionalEffectPosition = position;
            positionalEffectStartTime = time + NetConstants.Client_Delta;
            positionalEffectVector = StatusEffectFunctions.GetKnockedBackPositionVector(position, knockerPosition) * StatFunctions.ApplyResistanceMultiplier(knockbackResistance) * (NetConstants.Client_Delta / 1000f);
            AddClientStatusEffect(StatusEffect.KnockedBack, time, duration);
        }

        public void AddGrounded(Vec2 position, uint time, uint duration)
        {
            duration = StatFunctions.ApplyResistanceDuration(duration, currentSnapshot.GetAlternateStat(AlternateStatType.GroundedResistance));
            if (duration == 0) return;
            if (HasPositionalEffect(time)) return;
            positionalEffectCollided = false;
            positionalEffectPosition = position;
            positionalEffectStartTime = time + NetConstants.Client_Delta;
            positionalEffectVector = Vec2.zero;
            AddClientStatusEffect(StatusEffect.Grounded, time, duration);
        }

        private uint GetClientEffectEndTime(StatusEffect effect)
        {
            if (clientEffects.TryGetValue(effect, out var value))
                return value.endTime;
            return 0;
        }

        public void AddClientStatusEffect(StatusEffect effect, uint time, uint duration)
        {
            AdvanceHealth(time);
            uint newEndTime = time + duration;
            if (clientEffects.TryGetValue(effect, out var effectTime))
            {
                if (newEndTime < effectTime.endTime) return;
                effectTime.endTime = newEndTime;
                clientEffects[effect] = effectTime;
            }
            else
                clientEffects.Add(effect, new StatusEffectTime(time, newEndTime));
        }

        public bool HasClientEffect(StatusEffect effect, uint time)
        {
            if (clientEffects.TryGetValue(effect, out var effectTime))
            {
                return effectTime.HasEffect(time);
            }

            return false;
        }

        public bool HasEffect(StatusEffect effect, uint time)
        {
            return HasClientEffect(effect, time) || currentSnapshot.HasServerEffect(effect);
        }

        public void AddRage(uint time, float amount = 1, bool applyRageGainBonus = true)
        {
            if (HasEffect(StatusEffect.Mundane, time)) return;
            if (applyRageGainBonus)
                amount = StatFunctions.ApplyRageGainBonus(
                    amount,
                    currentSnapshot.GetAlternateStat(AlternateStatType.RageGain) + GetTimedAlternateStatBonus(AlternateStatType.RageGain));
            SetRage(rage + amount);
        }

        public void SetRage(float value)
        {
            rage = Math.Max(0f, Math.Min(value, 100f));
            SyncRageToClient();
        }

        public void ClearRage()
        {
            SetRage(0f);
        }

        private void SyncRageToClient()
        {
            player.rage.Value = rage;
        }

        private bool HasEnoughRageForAbility()
        {
            var rageIntegral = (int)Math.Floor(rage);
            if (rageIntegral <= 0) return false;

            switch ((ClassType)classInfo.id)
            {
                case ClassType.Lancer:
                {
                    var mods = SkillTreeFunctions.IsEnabled ? abilityMods : AbilityModifierSnapshot.Empty;
                    return rageIntegral >= AbilityFunctions.RageSpend.GetLancerRageCost(mods);
                }
                case ClassType.Minister:
                    return rageIntegral >= AbilityFunctions.Minister.GetRageCost(rageIntegral);
                case ClassType.Nomad:
                    return rageIntegral >= AbilityFunctions.Nomad.Ability_Cost;
                default:
                    return true;
            }
        }

        public void TriggerProcsFromDamageResult(DamageResult result, uint time, Vec2? hitTarget = null)
        {
            var procTrigger = ProcFunctions.HitResultToTrigger(result.type);
            if (procTrigger.HasValue)
                TriggerProcs(procTrigger.Value, time, hitTarget);

            if (result.wasCritical && result.type != HitResultType.Critical)
                TriggerProcs(ProcTrigger.CriticalStrike, time, hitTarget);
        }

        public void TriggerProcs(ProcTrigger trigger, uint time, Vec2? hitTarget = null)
        {
            triggeredProcKeys.Clear();

            for (int slot = 0; slot < 4; slot++)
            {
                var serverItem = player.GetItem(slot);
                var equipItem = serverItem?.itemData ?? Item.Blank;
                if (equipItem.IsBlank) continue;
                if (!(equipItem.GetInfo() is EquipmentInfo equip)) continue;

                for (int i = 0; i < equip.procs.Count; i++)
                {
                    var proc = equip.procs[i];
                    if (proc.trigger != trigger) continue;

                    int procKey = ProcFunctions.GetProcKey(equipItem.id, i);
                    if (!triggeredProcKeys.Add(procKey))
                        continue;

                    if (procCooldowns.TryGetValue(procKey, out var nextTime) && time < nextTime)
                        continue;

                    if (proc.statBonus != null)
                        ApplyProcStatBonus(proc.statBonus, time);
                    else if (proc.alternateStatBonus != null)
                        ApplyProcAlternateStatBonus(proc.alternateStatBonus, time);
                    else if (proc.rageGain != null)
                        AddRage(time, proc.rageGain.amount, applyRageGainBonus: false);

                    if (proc.aoe != null && ability != null)
                    {
                        var origin = player.position.Value;
                        if (proc.aoe.at != TalismanAoeAt.Target || hitTarget.HasValue)
                        {
                            var blastTarget = hitTarget ?? origin;
                            ability.FireAoe(proc.aoe, time, origin, blastTarget);
                        }
                    }

                    if (proc.cooldownMs > 0)
                        procCooldowns[procKey] = time + proc.cooldownMs;
                }
            }
        }

        public int GetAbilityCooldownMs(byte rageCost)
        {
            int cd = AbilityFunctions.GetAbilityCooldownMs(rageCost, classInfo.id);
            if (SkillTreeFunctions.IsEnabled)
            {
                if (abilityMods.cooldownMul > 0 && abilityMods.cooldownMul < 1)
                    cd = (int)(cd * abilityMods.cooldownMul);
                cd -= Math.Max(0, abilityMods.cooldownFlatMs);
            }
            return Math.Max(1, cd);
        }

        public void ApplyTalismanTimedStatBonus(StatType type, int amount, uint time, uint durationMs, int effectIndex)
        {
            ApplyTimedStatBonus(type, amount, time, durationMs, TimedStatSource.Talisman(effectIndex));
        }

        public void ApplyMinisterFieldStatBonus(StatType type, int amount, uint time, uint durationMs)
        {
            ApplyTimedStatBonus(type, amount, time, durationMs, TimedStatSource.MinisterField(type));
        }

        public void ApplyCommanderFieldStatBonus(StatType type, int amount, uint time, uint durationMs)
        {
            ApplyTimedStatBonus(type, amount, time, durationMs, TimedStatSource.CommanderField);
        }

        public void ApplyCommanderPulseStatBonus(StatType type, int amount, uint time, uint durationMs)
        {
            if (amount == 0 || durationMs == 0) return;

            var effect = ProcFunctions.GetStatBonusEffect(type);
            var hasEffect = effect.HasValue;
            uint newEnd = time + durationMs;

            for (int i = 0; i < timedStatBonuses.Count; i++)
            {
                var existing = timedStatBonuses[i];
                if (!existing.source.Equals(TimedStatSource.CommanderPulse) || existing.statType != type)
                    continue;

                player.AddStatBonus(type, amount);
                existing.amount += amount;
                existing.endTime = Math.Max(existing.endTime, newEnd);
                timedStatBonuses[i] = existing;

                if (hasEffect)
                    player.AddEffect(effect.Value, durationMs / 1000f);
                return;
            }

            ApplyTimedStatBonus(type, amount, time, durationMs, TimedStatSource.CommanderPulse);
        }

        public void ClearCommanderPulseBonuses()
        {
            for (int i = timedStatBonuses.Count - 1; i >= 0; i--)
            {
                if (timedStatBonuses[i].source.kind != 4) continue;

                var bonus = timedStatBonuses[i];
                player.RemoveStatBonus(bonus.statType, bonus.amount);

                if (bonus.hasEffect && !HasActiveTimedBonusEffect(bonus.effect, i))
                    player.RemoveEffect(bonus.effect);

                timedStatBonuses.RemoveAt(i);
            }
        }

        public void ApplyTimedStatBonus(StatType type, int amount, uint time, uint durationMs)
        {
            ApplyTimedStatBonus(type, amount, time, durationMs, TimedStatSource.ByStatType);
        }

        private void ApplyTimedStatBonus(StatType type, int amount, uint time, uint durationMs, TimedStatSource source)
        {
            if (amount == 0 || durationMs == 0) return;
            ApplyProcStatBonus(type, amount, durationMs, time, source);
        }

        public void ClearMinisterFieldBonuses()
        {
            for (int i = timedStatBonuses.Count - 1; i >= 0; i--)
            {
                if (timedStatBonuses[i].source.kind != 2) continue;

                var bonus = timedStatBonuses[i];
                player.RemoveStatBonus(bonus.statType, bonus.amount);

                if (bonus.hasEffect && !HasActiveTimedBonusEffect(bonus.effect, i))
                    player.RemoveEffect(bonus.effect);

                timedStatBonuses.RemoveAt(i);
            }
        }

        public void ClearTalismanTimedBonuses()
        {
            for (int i = timedStatBonuses.Count - 1; i >= 0; i--)
            {
                if (timedStatBonuses[i].source.kind != 1) continue;

                var bonus = timedStatBonuses[i];
                player.RemoveStatBonus(bonus.statType, bonus.amount);

                if (bonus.hasEffect && !HasActiveTimedBonusEffect(bonus.effect, i))
                    player.RemoveEffect(bonus.effect);

                timedStatBonuses.RemoveAt(i);
            }
        }

        public void ApplyTimedAlternateStatBonus(AlternateStatType type, int amount, uint time, uint durationMs)
        {
            if (amount == 0 || durationMs == 0) return;
            ApplyProcAlternateStatBonus(new ProcAlternateStatBonus(type, amount, durationMs), time);
        }

        public bool TryConsumeTalismanCooldown(int effectIndex, uint cooldownMs, uint time)
        {
            if (talismanCooldowns.TryGetValue(effectIndex, out var nextTime) && time < nextTime)
                return false;
            if (cooldownMs > 0)
                talismanCooldowns[effectIndex] = time + cooldownMs;
            return true;
        }

        public bool TryConsumeTalismanShotRage(int effectIndex, uint targetId, uint projectileStartTime)
        {
            long key = ((long)effectIndex << 32) ^ targetId;
            if (talismanShotRage.TryGetValue(key, out var lastShot) && lastShot == projectileStartTime)
                return false;
            talismanShotRage[key] = projectileStartTime;
            return true;
        }

        public bool TryConsumeLancerNovaHit(uint enemyId, uint burstTime)
        {
            if (lancerNovaHits.TryGetValue(enemyId, out var rec) && rec.burst == burstTime)
            {
                if (rec.count >= AbilityFunctions.Lancer.Nova_Hits_Per_Target)
                    return false;
                lancerNovaHits[enemyId] = (burstTime, rec.count + 1);
                return true;
            }
            lancerNovaHits[enemyId] = (burstTime, 1);
            return true;
        }

        private void ApplyProcStatBonus(ProcStatBonus bonus, uint time)
        {
            if (bonus == null) return;
            ApplyProcStatBonus(bonus.statType, bonus.amount, bonus.durationMs, time, TimedStatSource.ByStatType);
        }

        private void ApplyProcStatBonus(StatType statType, int amount, uint durationMs, uint time, TimedStatSource source)
        {
            if (amount == 0 || durationMs == 0) return;

            var effect = ProcFunctions.GetStatBonusEffect(statType);
            var hasEffect = effect.HasValue;
            uint newEnd = time + durationMs;

            for (int i = 0; i < timedStatBonuses.Count; i++)
            {
                var existing = timedStatBonuses[i];
                if (!existing.source.Equals(source) || existing.statType != statType) continue;

                int delta = amount - existing.amount;
                if (delta != 0)
                    player.AddStatBonus(statType, delta);

                existing.amount = amount;
                existing.endTime = Math.Max(existing.endTime, newEnd);
                timedStatBonuses[i] = existing;

                if (hasEffect)
                    player.AddEffect(effect.Value, durationMs / 1000f);
                return;
            }

            player.AddStatBonus(statType, amount);
            if (hasEffect)
                player.AddEffect(effect.Value, durationMs / 1000f);

            timedStatBonuses.Add(new TimedStatBonus
            {
                source = source,
                statType = statType,
                effect = effect ?? StatusEffect.VigorBonus,
                amount = amount,
                endTime = newEnd,
                hasEffect = hasEffect
            });
        }

        private void ApplyProcAlternateStatBonus(ProcAlternateStatBonus bonus, uint time)
        {
            if (bonus.amount == 0 || bonus.durationMs == 0) return;

            var effect = ProcFunctions.GetAlternateStatBonusEffect(bonus.statType);
            var hasEffect = effect.HasValue;

            if (bonus.statType == AlternateStatType.RateOfFire)
            {
                for (int i = 0; i < timedAlternateStatBonuses.Count; i++)
                {
                    var existing = timedAlternateStatBonuses[i];
                    if (existing.statType != AlternateStatType.RateOfFire) continue;

                    existing.amount = Math.Max(existing.amount, bonus.amount);
                    existing.endTime = Math.Max(existing.endTime, time + bonus.durationMs);
                    timedAlternateStatBonuses[i] = existing;
                    if (hasEffect)
                        player.AddEffect(effect.Value, bonus.durationMs / 1000f);
                    player.SetRateOfFireBonus(GetTimedAlternateStatBonus(AlternateStatType.RateOfFire, time));
                    return;
                }
            }

            if (bonus.statType == AlternateStatType.BlockChance)
            {
                for (int i = 0; i < timedAlternateStatBonuses.Count; i++)
                {
                    var existing = timedAlternateStatBonuses[i];
                    if (existing.statType != AlternateStatType.BlockChance) continue;

                    existing.amount = bonus.amount;
                    existing.endTime = Math.Max(existing.endTime, time + bonus.durationMs);
                    timedAlternateStatBonuses[i] = existing;
                    if (hasEffect)
                        player.AddEffect(effect.Value, bonus.durationMs / 1000f);
                    return;
                }
            }

            if (bonus.statType == AlternateStatType.AbsorptionChance)
            {
                for (int i = 0; i < timedAlternateStatBonuses.Count; i++)
                {
                    var existing = timedAlternateStatBonuses[i];
                    if (existing.statType != AlternateStatType.AbsorptionChance) continue;

                    existing.amount = bonus.amount;
                    existing.endTime = Math.Max(existing.endTime, time + bonus.durationMs);
                    timedAlternateStatBonuses[i] = existing;
                    if (hasEffect)
                        player.AddEffect(effect.Value, bonus.durationMs / 1000f);
                    return;
                }
            }

            if (hasEffect)
                player.AddEffect(effect.Value, bonus.durationMs / 1000f);

            timedAlternateStatBonuses.Add(new TimedAlternateStatBonus
            {
                statType = bonus.statType,
                effect = effect ?? StatusEffect.TrueBonus,
                amount = bonus.amount,
                endTime = time + bonus.durationMs,
                hasEffect = hasEffect
            });

            if (bonus.statType == AlternateStatType.RateOfFire)
                player.SetRateOfFireBonus(GetTimedAlternateStatBonus(AlternateStatType.RateOfFire, time));
            else
                player.SyncCombatSnapshotEquipment();
        }

        private void AdvanceTimedBonuses(uint time)
        {
            for (int i = timedAlternateStatBonuses.Count - 1; i >= 0; i--)
            {
                if (time < timedAlternateStatBonuses[i].endTime) continue;

                var bonus = timedAlternateStatBonuses[i];
                if (bonus.hasEffect && !HasActiveTimedAlternateBonusEffect(bonus.effect, i))
                    player.RemoveEffect(bonus.effect);

                timedAlternateStatBonuses.RemoveAt(i);
                if (bonus.statType == AlternateStatType.RateOfFire)
                    player.SetRateOfFireBonus(GetTimedAlternateStatBonus(AlternateStatType.RateOfFire, time));
                else
                    player.SyncCombatSnapshotEquipment();
            }

            for (int i = timedStatBonuses.Count - 1; i >= 0; i--)
            {
                if (time < timedStatBonuses[i].endTime) continue;

                var bonus = timedStatBonuses[i];
                player.RemoveStatBonus(bonus.statType, bonus.amount);

                if (bonus.hasEffect && !HasActiveTimedBonusEffect(bonus.effect, i))
                    player.RemoveEffect(bonus.effect);

                timedStatBonuses.RemoveAt(i);
            }
        }

        private bool HasActiveTimedAlternateBonusEffect(StatusEffect effect, int excludeIndex)
        {
            for (int i = 0; i < timedAlternateStatBonuses.Count; i++)
            {
                if (i == excludeIndex) continue;
                if (timedAlternateStatBonuses[i].hasEffect && timedAlternateStatBonuses[i].effect == effect)
                    return true;
            }
            return false;
        }

        private bool HasActiveTimedBonusEffect(StatusEffect effect, int excludeIndex)
        {
            for (int i = 0; i < timedStatBonuses.Count; i++)
            {
                if (i == excludeIndex) continue;
                if (timedStatBonuses[i].hasEffect && timedStatBonuses[i].effect == effect)
                    return true;
            }
            return false;
        }
    }
}
