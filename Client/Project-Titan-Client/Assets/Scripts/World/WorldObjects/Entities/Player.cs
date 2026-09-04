using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Components;
using TitanCore.Data.Components.Projectiles;
using TitanCore.Data.Items;
using TitanCore.Data.Map;
using TitanCore.Net;
using TitanCore.Net.Packets.Client;
using TitanCore.Net.Packets.Models;
using UnityEngine;
using Utils.NET.Algorithms;
using Utils.NET.Geometry;
using Utils.NET.Logging;

public class Player : Character
{

    private static Color soulProgressGainedColor = new Color(0.2196079f, 0.8313726f, 0.8313726f, 1f);

    public override GameObjectType ObjectType => GameObjectType.Player;

    public bool attacking = false;

    private uint projIds = 0;

    public float health;

    public int cooldown = 1;

    public int cooldownDuration = 1;

    private uint warriorAbilityEndTime;

    private uint warriorNextPulseTime;

    private bool wantsToUseAbility = false;

    private TileInfo currentTile;

    public Vector2 aimPosition;

    public Vector2 abilityAimPosition;

    public float rage = 0;

    public byte abilityValue = 0;

    private Vec2 positionalEffectVector;

    private bool positionalEffectCollided = false;

    private DateTime lastEmote;

    public Item[] backpack;

    public uint skillTreeRanks;

    public Item socketedTalisman = Item.Blank;

    private float abilityActivationRage;

    private readonly Dictionary<long, uint> hitMarkedTalismanShots = new Dictionary<long, uint>();

    private readonly Dictionary<uint, (uint burst, int count)> lancerNovaHits = new Dictionary<uint, (uint, int)>();

    public Vec2 lastSentPosition;

    public uint lastSentPositionTime;

    private Option showPlayerName;

    public uint target;

    private NomadTarget targetEffect;

    private Vector3 lastFixedMove;

    private float moveAngle = 0;

    private bool walk = false;

    private bool moving = false;

    public Vector3 targetUpdatePosition;

    private uint lastMovementTime;

    private float targetUpdateTime;

    public int lockedMaxHealth;

    public int lockedSpeed;

    public int lockedAttack;

    public int lockedDefense;

    public int lockedVigor;

    public int soulGoal;


    protected override bool IsAttacking => attacking && WeaponEquipped;

    public bool WeaponEquipped => !items[0].IsBlank && items[0].GetInfo() is WeaponInfo;

    protected override void Awake()
    {
        base.Awake();

        showPlayerName = Options.Get(OptionType.ShowPlayerName);
    }

    public override void Enable()
    {
        base.Enable();

        attacking = false;
        projIds = 0;
        fullSouls = 0;
        health = 0;
        currentTile = null;
        rage = 0;
        abilityValue = 0;
        world.gameManager.ui.OnSoulsUpdated(fullSouls, soulGoal);
        target = 0;
        moveAngle = 0;
        walk = false;
        moving = false;

        lockedMaxHealth = 0;
        lockedSpeed = 0;
        lockedAttack = 0;
        lockedDefense = 0;
        lockedVigor = 0;

        soulGoal = 0;

        cooldown = 1;
        cooldownDuration = 1;
        warriorAbilityEndTime = 0;
        warriorNextPulseTime = 0;
        backpack = new Item[8];
        skillTreeRanks = 0;
        socketedTalisman = Item.Blank;

        wantsToUseAbility = false;
        currentTile = null;

        OnShowPlayerName(showPlayerName.GetBool());
        showPlayerName.AddBoolCallback(OnShowPlayerName);
    }

    public override void Disable()
    {
        base.Disable();

        ReturnTarget();

        showPlayerName.RemoveBoolCallback(OnShowPlayerName);
    }

    private void OnShowPlayerName(bool value)
    {
        UpdateNameLabel();
    }

    protected override void OnAllyTransparency(float value)
    {
        
    }

    protected override void UpdateNameLabel()
    {
        if (!showPlayerName.GetBool())
        {
            ShowGroundLabel(null);
            return;
        }

        base.UpdateNameLabel();
    }

    public override void LoadObjectInfo(GameObjectInfo info)
    {
        base.LoadObjectInfo(info);
    }

    protected override Item[] CreateItems()
    {
        return new Item[12];
    }

    public override void NetUpdate(NetStat[] stats, bool first)
    {
        base.NetUpdate(stats, first);

        health = Mathf.Min(health, GetStatFunctional(StatType.MaxHealth));
        world.gameManager.ui.OnPlayerStatsUpdated(this);
        world.gameManager.ui.SetHealthValue((int)health);
    }

    protected override void ProcessStat(NetStat stat, bool first)
    {
        base.ProcessStat(stat, first);
        switch (stat.type)
        {
            case ObjectStatType.Health:
                if (first)
                {
                    health = (int)stat.value;
                }
                break;
            case ObjectStatType.Souls:
                world.gameManager.ui.OnSoulsUpdated(fullSouls, soulGoal);
                break;
            case ObjectStatType.SoulGoal:
                soulGoal = (int)stat.value;
                world.gameManager.ui.OnSoulsUpdated(fullSouls, soulGoal);
                break;
            case ObjectStatType.PremiumCurrency:
                world.gameManager.ui.SetPremiumCurrency((long)stat.value);
                break;
            case ObjectStatType.DeathCurrency:
                world.gameManager.ui.SetDeathCurrency((long)stat.value);
                break;
            case ObjectStatType.Rage:
                rage = (float)stat.value;
                break;
            case ObjectStatType.Heal:
                var healAmount = (int)stat.value;
                var realizedHeal = (int)Mathf.Min(healAmount, GetStatFunctional(StatType.MaxHealth) - health);
                health += healAmount;
                if (realizedHeal <= 0) break;
                ShowAlert("+" + realizedHeal, Color.green);
                break;
            case ObjectStatType.Backpack0:
            case ObjectStatType.Backpack1:
            case ObjectStatType.Backpack2:
            case ObjectStatType.Backpack3:
            case ObjectStatType.Backpack4:
            case ObjectStatType.Backpack5:
            case ObjectStatType.Backpack6:
            case ObjectStatType.Backpack7:
                backpack[(int)stat.type - (int)ObjectStatType.Backpack0] = (Item)stat.value;
                break;
            case ObjectStatType.Target:
                ReturnTarget();
                target = (uint)stat.value;
                if (target == 0)
                {
                    ReturnTarget();
                    break;
                }
                if (world.TryGetObject(target, out var targetObj))
                    UpdateTarget(targetObj);
                break;
            case ObjectStatType.MaxHealthLock:
                lockedMaxHealth = (int)stat.value;
                break;
            case ObjectStatType.SpeedLock:
                lockedSpeed = (int)stat.value;
                break;
            case ObjectStatType.AttackLock:
                lockedAttack = (int)stat.value;
                break;
            case ObjectStatType.DefenseLock:
                lockedDefense = (int)stat.value;
                break;
            case ObjectStatType.VigorLock:
                lockedVigor = (int)stat.value;
                break;
        }
    }

    public void ReturnTarget()
    {
        if (targetEffect != null)
        {
            world.effectManager.ReturnEffect(targetEffect);
            targetEffect = null;
        }
    }

    public void UpdateTarget(WorldObject obj)
    {
        ReturnTarget();
        targetEffect = (NomadTarget)world.PlayEffect(EffectType.NomadTarget, obj.transform.position);
        targetEffect.SetFollow(obj.transform);
    }

    public void ConsumeTarget()
    {
        AddRage(5, false);
        target = 0;
        if (targetEffect != null)
        {
            targetEffect.Consume();
            targetEffect = null;
        }
    }

    protected override void SetCurrentTile(TileInfo tileInfo)
    {
        base.SetCurrentTile(tileInfo);

        if (tileInfo == null || tileInfo.music == null) return;
        if (!world.dynamicMusic || !AudioManager.TryGetSound(tileInfo.music, out var music)) return;
        world.PlayMusic(music);
    }

    public override Item GetItem(int index)
    {
        if (SkillTreeFunctions.IsEnabled && index == SkillTreeFunctions.Talisman_Slot)
            return socketedTalisman;
        if (index < 12)
            return items[index];
        return base.GetItem(index);
    }

    public override void SetItem(int index, Item item)
    {
        if (SkillTreeFunctions.IsEnabled && index == SkillTreeFunctions.Talisman_Slot)
        {
            socketedTalisman = item;
            RaiseInventoryUpdated();
            return;
        }
        if (index < 4)
            base.SetItem(index, item);
        else if (index < 12)
            items[index] = item;
        RaiseInventoryUpdated();
    }

    public override SlotType GetSlotType(int index)
    {
        if (SkillTreeFunctions.IsEnabled && index == SkillTreeFunctions.Talisman_Slot)
            return SlotType.Talisman;
        return base.GetSlotType(index);
    }

    public void SetAttacking(bool attacking, float attackAngle, Vector2 aimPosition)
    {
        this.attacking = attacking;
        this.attackAngle = attackAngle;
        this.aimPosition = aimPosition;
    }

    public void SetAbilityAimPosition(Vector2 abilityAimPosition)
    {
        this.abilityAimPosition = abilityAimPosition;
    }

    public float GetAttackAngle() => attackAngle;

    public void SetMove(float moveAngle, bool walk, bool moving)
    {
        this.moveAngle = moveAngle;
        this.walk = walk;
        this.moving = moving;

        if (world != null && world.stopTick) return;
        if (moving && !HasPositionalEffect() && !HasPositionalEffect(world.clientTime - NetConstants.Client_Delta) && lastFixedMove != targetUpdatePosition)
        {
            float t = Mathf.Clamp01((Time.time - targetUpdateTime) / (NetConstants.Client_Delta / 1000f));
            Position = lastFixedMove + (targetUpdatePosition - lastFixedMove) * t;
        }
    }

    private Vector3 Move(float delta)
    {
        if (world.stopTick || HasPositionalEffect()) return lastFixedMove;

        float tilesPerSecond = StatFunctions.TilesPerSecond(GetStatFunctional(StatType.Speed), HasStatusEffect(StatusEffect.Slowed), HasStatusEffect(StatusEffect.Speedy)) * delta * (currentTile?.speed ?? 1);
        if (walk)
            tilesPerSecond *= 0.5f;

        Vector3 moveVector = new Vector3(Mathf.Cos(moveAngle), Mathf.Sin(moveAngle), 0) * tilesPerSecond;
        var position = lastFixedMove;
        var newPosition = position + moveVector;

        bool xOk = CanMoveTo(newPosition.x, position.y);
        bool yOk = CanMoveTo(position.x, newPosition.y);

        if (xOk && yOk)
        {
            if (CanMoveTo(newPosition.x, newPosition.y))
            {
                return newPosition;
            }
        }

        if (xOk)
        {
            return new Vector3(newPosition.x, position.y);
        }
        else if (yOk)
        {
            return new Vector3(position.x, newPosition.y);
        }

        return position;
    }

    private bool CanMoveTo(float x, float y)
    {
        if (world.collision.PlayerCollides(x, y))
            return false;
        if (!world.tilemapManager.CanWalkOn(x, y))
            return false;
        if (!LineOkay(x, y))
            return false;
        return true;
    }

    private bool LineOkay(float x, float y)
    {
        foreach (var position in Bresenham.Line(lastSentPosition.ToInt2(), new Int2((int)x, (int)y)))
            if (world.collision.IsWall(position.x, position.y))
                return false;
        return true;
    }

    public override Vector2 GetPosition()
    {
        return Position;
    }

    protected override Vector2 GetPositionVisual()
    {
        return Position;
    }

    public override void SetPosition(Vec2 position, bool first)
    {
        if (!first) return;
        Position = position.ToVector2();
        lastFixedMove = position.ToVector2();
        targetUpdatePosition = lastFixedMove;
        targetUpdateTime = Time.time;// + 0.016f;
    }

    public override void WorldFixedUpdate(uint time, uint delta)
    {
        base.WorldFixedUpdate(time, delta);

        if (health < GetStatFunctional(StatType.MaxHealth))
        {
            var regen = StatFunctions.HealthRegen(GetStatFunctional(StatType.Vigor), (int)delta, HasStatusEffect(StatusEffect.Healing), HasStatusEffect(StatusEffect.Sick));
            health += regen;
            if (health > GetStatFunctional(StatType.MaxHealth))
                health = GetStatFunctional(StatType.MaxHealth);
            world.gameManager.ui.SetHealthValue((int)health);

            //Debug.Log($"Time: {time / 16}, HP: {health}");
        }

        //Debug.Log(world.clientTickId);
        //Debug.Log(health);

        UpdateMovement(time);

        if (cooldown < cooldownDuration)
        {
            cooldown += (int)delta;
        }

        if (wantsToUseAbility)
        {
            wantsToUseAbility = false;
            DoUseAbility();
        }

        if (attacking)
        {
            Shoot(time);
        }

        UpdateCharacterAnimation();

        targetUpdateTime = Time.time;// + 0.016f;
    }

    public void UpdateMovement(uint time)
    {
        if (time == lastMovementTime) return;
        lastMovementTime = time;

        if (HasPositionalEffect())
        {
            UpdatePositionalEffect(16 / 1000f);
            UpdateTargetUpdatePosition();
        }
        else if (moving)
        {
            Position = Move(16 / 1000f);
            UpdateTargetUpdatePosition();
        }
        else if ((Vector2)Position != (Vector2)targetUpdatePosition)
        {
            Position = targetUpdatePosition;
            lastFixedMove = targetUpdatePosition;
        }
    }

    protected override void Update()
    {
        base.Update();

        //UpdatePositionalEffect();
    }

    public void Goto(Vec2 position)
    {
        Position = position.ToVector2();
        UpdateTargetUpdatePosition();
        targetUpdateTime = Time.time;// + 0.016f;
    }

    public Vec2 GetNetworkPosition()
    {
        return new Vec2(lastFixedMove.x, lastFixedMove.y);
    }

    private void UpdateTargetUpdatePosition()
    {
        lastFixedMove = Position;
        targetUpdatePosition = Move(16 / 1000f);
    }

    private void Shoot(uint time)
    {
        if (world.stopTick || HasClientEffect(StatusEffect.Dashing) || HasClientEffect(StatusEffect.Grounded) || HasClientEffect(StatusEffect.KnockedBack)) return;

        var item = items[0];
        if (item.IsBlank) return;
        var itemInfo = item.GetInfo();
        if (!(itemInfo is WeaponInfo weaponInfo)) return;
        if (shootCooldown < 0)
        {
            shootCooldown = StatFunctions.GetShootCooldownMs(weaponInfo.rateOfFire, GetAlternateStatIncrease(AlternateStatType.RateOfFire));
            //Debug.Log($"Time: {world.clientTime} Next Shoot: {world.clientTime + shootCooldown}");

            if (animation is CharacterAnimationData characterAnimation)
                characterAnimation.attackFps = (shootCooldown / 2) / 1000.0f;

            var pos = GetNetworkPosition();
            var target = aimPosition.ToVec2();
            var vector = target - pos;
            var length = vector.Length;
            if (length > 6)
                target = pos + vector.ChangeLength(6, length); // provide current length to prevent a second Sqrt call

            world.gameManager.client.SendAsync(new TnShoot(world.clientTickId, projIds, target, pos));
            projIds = ShootWeapon(item, weaponInfo, target, projIds, pos, time);

            PlaySfxType(SfxType.Shoot);
        }
    }

    private bool HasPositionalEffect(uint worldTime = 0)
    {
        return HasClientEffect(StatusEffect.Charmed, worldTime) || HasClientEffect(StatusEffect.Dashing, worldTime) || HasClientEffect(StatusEffect.KnockedBack, worldTime) || HasClientEffect(StatusEffect.Grounded, worldTime);
    }

    private void UpdatePositionalEffect(float delta)
    {
        UpdatePositional(delta);
    }

    private void AddPositionalEffect(Vec2 vector)
    {
        positionalEffectCollided = false;
        positionalEffectVector = vector;
    }

    private void UpdatePositional(float delta)
    {
        if (!positionalEffectCollided)
        {
            var newPosition = (Vector2)Position + positionalEffectVector.ToVector2() * delta;
            if (!CanMoveTo(newPosition.x, newPosition.y))
                positionalEffectCollided = true;
            else
                Position = newPosition;
        }
    }

    public uint ShootWeapon(Item item, WeaponInfo weapon, Vec2 target, uint projId, Vec2 position, uint time, Action<Projectile> processor = null, float angleOffset = 0)
    {
        var projData = weapon.projectiles[projId % weapon.projectiles.Length];
        bool isPlayer = this is Player;

        var length = position.DistanceTo(target);
        var shootAngle = position.AngleTo(target) + angleOffset;
        foreach (var angle in NetConstants.GetProjectileAngles(shootAngle, projData.angleGap, projData.amount))
        {
            uint id = projId++;
            projData = weapon.projectiles[id % weapon.projectiles.Length];
            ushort damage = PlayerDamage(item.enchantType, item.enchantLevel, weapon.slotType, projData, id);
            if (projData.Type == ProjectileType.Aoe)
            {
                var aoeData = (AoeProjectileData)projData;
                var aoe = (ItemAoeProjectile)world.PlayEffect(EffectType.ItemAoeProjectile, position.ToVector2());
                aoe.Setup(Ally ? world.hittables : world.enemyHittables, time, aoeData, position.ToVector2(), (position + Vec2.FromAngle(angle) * length).ToVector2(), damage, id, true, weapon);
                aoe.players = isPlayer;
                aoe.SetHitSfx("break_potion");
                world.AddAoeProjectile(aoe);
            }
            else
            {
                var proj = world.gameManager.objectManager.GetProjectile();
                proj.Setup(Ally ? world.hittables : world.enemyHittables, position.ToVector2(), projData, angle, id, time, damage, HasStatusEffect(StatusEffect.Reach));
                proj.players = isPlayer;
                processor?.Invoke(proj);
            }
        }
        return projId;
    }

    private ushort PlayerDamage(ItemEnchantType enchant, int enchantLevel, SlotType slotType, ProjectileData data, uint id)
    {
        var rand = world.GetRand(id);
        WeaponFunctions.GetProjectileDamage(slotType, data, out var minDamage, out var maxDamage);
        var damage = (int)((minDamage + (ushort)((maxDamage - minDamage) * rand)) * StatFunctions.AttackModifier(GetStatFunctional(StatType.Attack), HasStatusEffect(StatusEffect.Damaging)));
        if (enchant == ItemEnchantType.Damaging)
            damage = (int)(damage * EnchantFunctions.Damage(enchantLevel));
        return (ushort)damage;
    }

    private int GetHeldWeaponVolleyDamage(uint projectileId)
    {
        var item = GetItem(0);
        if (item.IsBlank || !(item.GetInfo() is WeaponInfo weaponInfo) || weaponInfo.projectiles == null || weaponInfo.projectiles.Length == 0)
            return 1;

        int shotCount = WeaponFunctions.GetVolleyShotCount(weaponInfo.projectiles);
        int total = 0;
        for (int i = 0; i < shotCount; i++)
        {
            var shot = weaponInfo.projectiles[i % weaponInfo.projectiles.Length];
            total += PlayerDamage(item.enchantType, item.enchantLevel, weaponInfo.slotType, shot, projectileId + (uint)i);
        }
        return Mathf.Max(1, total);
    }

    public void PositionSent(Vec2 position)
    {
        Int2 tilePos = position;
        var tile = world.tilemapManager.GetTileType(tilePos.x, tilePos.y);
        if (tile == 0)
            currentTile = null;
        else
            currentTile = (TileInfo)GameData.objects[tile];

        var time = world.clientTime;
        var timeDif = time - lastSentPositionTime;
        var tps = lastSentPosition.DistanceTo(position) / (timeDif / 1000f);

        lastSentPositionTime = time;
        lastSentPosition = position;
    }

        public DamageResult ResolveOutgoingDamage(int rawDamage, int targetDefense, bool targetFortified, uint projectileId, uint time, uint targetId, int targetDefenseMinusAmount = 0)
        {
            return StatFunctions.ResolveOutgoingDamage(
                rawDamage,
                GetEquipItems(),
                0,
                0,
                targetDefense,
                targetFortified,
                projectileId,
                time,
                targetId,
                gameId,
                targetDefenseMinusAmount);
        }

    public override bool IsHitBy(Vec2 position, Projectile projectile, out bool killed)
    {
        bool hit = base.IsHitBy(position, projectile, out killed);
        if (hit && !HasStatusEffect(StatusEffect.Invulnerable))
        {
            var pos = GetNetworkPosition();
            var result = ResolveIncomingDamage(projectile.damage, projectile.projId, world.clientTime);
            ApplyCombatResult(result);
            ApplyOnHitEffects(projectile.data.onHitEffects, pos, position);
            world.gameManager.client.SendAsync(new TnHit(world.clientTickId, projectile.projId, gameId, pos));
        }
        return hit;
    }

    public override void HitBy(AoeProjectile projectile)
    {
        base.HitBy(projectile);

        if (IsInvincible() || HasStatusEffect(StatusEffect.Invulnerable)) return;

        var pos = GetNetworkPosition();
        var result = ResolveIncomingDamage(projectile.damage, projectile.projId, world.clientTime);
        ApplyCombatResult(result);
        ApplyOnHitEffects(projectile.aoeData.onHitEffects, pos, Vec2.zero);
        world.gameManager.client.SendAsync(new TnHit(world.clientTickId, projectile.projId, gameId, pos));
    }

    private void ApplyCombatResult(DamageResult result)
    {
        if (result.type == HitResultType.Blocked)
        {
            ShowAlert("Blocked", CombatDisplay.TrueDamageColor, true);
            AudioManager.PlaySound("break_potion");
            return;
        }

        if (result.type == HitResultType.Absorbed)
        {
            ShowAlert("ABSORBED", CombatDisplay.AbsorbedColor, true);
            AudioManager.PlaySound("heal");
            health = Mathf.Min(health - result.damage, GetStatFunctional(StatType.MaxHealth));
            world.gameManager.ui.SetHealthValue((int)health);
            return;
        }

        if (result.damage <= 0) return;

        CombatDisplay.ShowHitResult(this, result);
        PlayHurt();
        health -= result.damage;
        world.gameManager.ui.SetHealthValue((int)health);
        world.gameManager.ui.PlayerDamageTaken();
    }

    private void PlayHurt()
    {
        AudioManager.PlaySound("hurt-" + UnityEngine.Random.Range(1, 6));
    }

    private void ApplyOnHitEffects(StatusEffectData[] onHits, Vec2 position, Vec2 projPos)
    {
        if (onHits.Length == 0) return;
        for (int i = 0; i < onHits.Length; i++)
        {
            var onHit = onHits[i];
            switch (onHit.type)
            {
                case StatusEffect.Charmed:
                    ShowAlert("Charmed", Color.red, true);
                    AddCharmed(position, projPos, onHit.duration);
                    break;
                case StatusEffect.KnockedBack:
                    ShowAlert("KnockedBack", Color.red, true);
                    AddKnockedBack(position, projPos, onHit.duration);
                    break;
                case StatusEffect.Grounded:
                    ShowAlert("Grounded", Color.red, true);
                    AddGrounded(position, onHit.duration);
                    break;
                case StatusEffect.Slowed:
                    ShowAlert("Slowed", Color.red, true);
                    AddClientEffect(onHit.type, onHit.duration);
                    break;
                case StatusEffect.Mundane:
                    rage = 0;
                    AddClientEffect(onHit.type, onHit.duration);
                    break;
                default:
                    AddClientEffect(onHit.type, onHit.duration);
                    break;
            }
        }
    }

    private void AddCharmed(Vec2 position, Vec2 charmerPosition, uint duration)
    {
        if (HasPositionalEffect()) return;
        AddPositionalEffect(StatusEffectFunctions.GetCharmedPositionVector(position, charmerPosition));
        AddClientEffect(StatusEffect.Charmed, duration);
    }

    private void AddKnockedBack(Vec2 position, Vec2 knockerPosition, uint duration)
    {
        var knockbackResistance = GetAlternateStatIncrease(AlternateStatType.KnockbackResistance);
        if (knockbackResistance >= 100) return;
        if (HasPositionalEffect()) return;
        AddPositionalEffect(StatusEffectFunctions.GetKnockedBackPositionVector(position, knockerPosition) * StatFunctions.ApplyResistanceMultiplier(knockbackResistance));
        AddClientEffect(StatusEffect.KnockedBack, duration);
    }

    private void AddGrounded(Vec2 position, uint duration)
    {
        duration = StatFunctions.ApplyResistanceDuration(duration, GetAlternateStatIncrease(AlternateStatType.GroundedResistance));
        if (duration == 0) return;
        if (HasPositionalEffect()) return;
        AddPositionalEffect(Vec2.zero);
        AddClientEffect(StatusEffect.Grounded, duration);
    }

    private void AddDash(Vec2 position, Vec2 target, int rage, uint durationMs, float extraDistance)
    {
        AddPositionalEffect(AbilityFunctions.BladeWeaver.GetDashPositionVector(position.AngleTo(target), rage, durationMs, extraDistance));
    }

    public void IncrementAbilityValue()
    {
        switch ((ClassType)info.id)
        {
            case ClassType.Brewer:
                abilityValue = (byte)((abilityValue + 1) % 2);

                var pos = Position;
                pos.z = 0;
                var brewerEffect = (BerserkerAbility)world.PlayEffect(EffectType.BrewerSelection, pos);
                brewerEffect.SetSprite(TextureManager.GetSprite("BrewerPotion-" + (abilityValue + 1)));
                break;
        }
    }

    public void SetAbilityValue(byte value)
    {
        abilityValue = value;
    }

    public void UseAbility(bool first)
    {
        if (cooldown < cooldownDuration)
        {
            if (first)
                world.GameChat("Ability is still on cooldown", ChatType.Error);
            return;
        }

        if (!HasEnoughRageForAbility())
        {
            if (first)
                world.GameChat("Not enough rage available! Attack enemies to gain more.", ChatType.Error);
            return;
        }

        wantsToUseAbility = true;
    }

    private void DoUseAbility()
    {
        if (cooldown < cooldownDuration)
        {

            return;
        }

        if (HasPositionalEffect())
        {

            return;
        }

        if (!HasEnoughRageForAbility())
        {

            return;
        }

        abilityActivationRage = Mathf.Floor(rage);

        Vec2 position = GetNetworkPosition();
        var target = abilityAimPosition == Vector2.zero ? aimPosition.ToVec2() : abilityAimPosition.ToVec2();
        abilityAimPosition = Vector2.zero;

        switch ((ClassType)info.id)
        {
            case ClassType.Warrior:
                //world.PlayWarriorAbilityEffect(new WarriorAbilityWorldEffect(gameId, position, (byte)rage, GetStatFunctional(StatType.Attack)));
                var warriorMods = BuildClientAbilityModifiers();
                warriorAbilityEndTime = world.clientTime + AbilityFunctions.Warrior.GetAbilityDuration(0) + (uint)Mathf.Max(0, warriorMods.durationBonusMs);
                warriorNextPulseTime = 0;
                var blast = (AreaBlast)world.PlayEffect(EffectType.AreaBlast, position.ToVector2());
                blast.SetInfo(2, GetTalismanAbilityAoeColor(Color.white));
                SpendDumpAbilityRage();
                break;
            case ClassType.Alchemist:
                var alchemistFx = new AlchemistAbilityWorldEffect(gameId, target, (byte)Mathf.Floor(rage), GetStatFunctional(StatType.Attack));
                ColorFromTalisman(alchemistFx);
                world.PlayAlchemistAbilityEffect(alchemistFx);
                SpendDumpAbilityRage();
                break;
            case ClassType.Lancer:
                var lancerItem = new Item(AbilityFunctions.Lancer.Ability_Item_Id);
                var lancerMods = SkillTreeFunctions.IsEnabled ? BuildClientAbilityModifiers() : AbilityModifierSnapshot.Empty;
                int lancerRage = Mathf.FloorToInt(rage);
                float lancerAim = position.AngleTo(target);
                foreach (var novaAngle in AbilityFunctions.Lancer.GetNovaAngles(lancerAim))
                {
                    projIds = ShootWeapon(lancerItem, (WeaponInfo)lancerItem.GetInfo(), target, projIds, position, world.clientTime, proj =>
                    {
                        int weaponDmg = 1;
                        var held = GetItem(0);
                        if (!held.IsBlank && held.GetInfo() is WeaponInfo heldWep && heldWep.projectiles != null && heldWep.projectiles.Length > 0)
                            weaponDmg = PlayerDamage(held.enchantType, held.enchantLevel, heldWep.slotType, heldWep.projectiles[0], proj.projId);
                        int dmg = AbilityFunctions.Lancer.ScaleWeaponDamage(weaponDmg);
                        dmg = AbilityFunctions.RageSpend.ApplyRageDamageMul(dmg, lancerRage);
                        dmg = (int)(dmg * (1f + lancerMods.abilityDamagePct));
                        proj.damage = (ushort)Mathf.Max(1, dmg);
                        proj.pierceThrough = AbilityFunctions.Lancer.RollsPierce(lancerMods.pierceChance, proj.projId, gameId);
                        proj.grantsRage = false;
                        proj.lancerNova = true;
                        if (lancerMods.projectileSizePct > 0)
                            proj.SetSize(proj.data.size * (1f + lancerMods.projectileSizePct));
                        proj.AddRangeTiles(lancerMods.abilityRangeBonus);
                    }, novaAngle - lancerAim);
                }
                SpendFixedAbilityRage(GetLancerAbilityRageCost());
                break;
            case ClassType.Commander:
                var commanderFx = new CommanderAbilityWorldEffect(gameId, position, (byte)Mathf.Floor(rage), GetStatFunctional(StatType.Attack));
                ColorFromTalisman(commanderFx);
                world.PlayCommanderAbilityEffect(commanderFx);
                SpendDumpAbilityRage();
                break;
            case ClassType.Minister:
                var cost = AbilityFunctions.Minister.GetRageCost((int)Mathf.Floor(rage));
                var ministerFx = new MinisterAbilityWorldEffect(gameId, position, cost, GetStatFunctional(StatType.Attack));
                ColorFromTalisman(ministerFx);
                world.PlayMinisterAbilityEffect(ministerFx);
                SpendFixedAbilityRage(cost);
                break;
            case ClassType.Berserker:
                var berserkerMods = BuildClientAbilityModifiers();
                rateOfFireBonus = Mathf.Max(rateOfFireBonus, AbilityFunctions.Berserker.RoF_Amount + berserkerMods.rofAmount);
                var berserkerFx = new BerserkerAbilityWorldEffect(gameId, position, position.AngleTo(target) * AngleUtils.Rad2Deg, (byte)Mathf.Floor(rage), GetStatFunctional(StatType.Attack));
                ColorFromTalisman(berserkerFx);
                world.PlayBerserkerAbilityEffect(berserkerFx);
                SpendDumpAbilityRage();
                break;
            case ClassType.Ranger:
                world.PlayEffect(EffectType.RangerArrowsShoot, position.ToVector2());
                SpendDumpAbilityRage();
                break;
            case ClassType.Brewer:
                AudioManager.PlaySound("drink_potion");
                var brewerMods = BuildClientAbilityModifiers();
                if (abilityValue == 0)
                    rateOfFireBonus = Mathf.Max(rateOfFireBonus, AbilityFunctions.Brewer.RoF_Amount + brewerMods.rofAmount);
                var brewerFx = new BrewerAbilityWorldEffect(gameId, position, (byte)Mathf.Floor(rage), GetStatFunctional(StatType.Attack), abilityValue);
                ColorFromTalisman(brewerFx);
                world.PlayBrewerAbilityEffect(brewerFx);
                SpendDumpAbilityRage();
                break;
            case ClassType.Bladeweaver:
                var rageToUse = abilityValue;
                var bladeweaverMods = BuildClientAbilityModifiers();
                uint dashDuration = AbilityFunctions.BladeWeaver.Dash_Duration + (uint)Mathf.Max(0, bladeweaverMods.durationBonusMs);
                var bladeweaverFx = new BladeweaverAbilityWorldEffect(gameId, dashDuration);
                ColorFromTalisman(bladeweaverFx);
                world.PlayBladeweaverAbilityEffect(bladeweaverFx);
                AddDash(position, target, rageToUse, dashDuration, bladeweaverMods.abilityRangeBonus);

                var bwItem = new Item(0x2a8);
                projIds = ShootWeapon(bwItem, (WeaponInfo)bwItem.GetInfo(), target, projIds, position, world.clientTime, proj =>
                {
                    int weaponDamage = GetHeldWeaponVolleyDamage(projIds);
                    int dmg = AbilityFunctions.BladeWeaver.ScaleWeaponDamage(weaponDamage, rageToUse);
                    dmg = (int)(dmg * (1f + bladeweaverMods.abilityDamagePct));
                    proj.damage = (ushort)Mathf.Max(1, dmg);
                });
                SpendFixedAbilityRage(rageToUse);
                break;
            case ClassType.Nomad:
                var nomadFx = new NomadAbilityWorldEffect(gameId, target);
                ColorFromTalisman(nomadFx);
                world.PlayNomadAbilityEffect(nomadFx);
                SpendFixedAbilityRage(AbilityFunctions.Nomad.Ability_Cost);
                break;
        }

        cooldownDuration = AbilityFunctions.GetAbilityCooldownMs((byte)Mathf.Floor(rage), info.id);
        if (SkillTreeFunctions.IsEnabled)
        {
            var mods = BuildClientAbilityModifiers();
            if (mods.cooldownMul > 0 && mods.cooldownMul < 1)
                cooldownDuration = Mathf.Max(1, (int)(cooldownDuration * mods.cooldownMul));
            cooldownDuration = Mathf.Max(1, cooldownDuration - Mathf.Max(0, mods.cooldownFlatMs));
        }
        cooldown = 0;

        world.gameManager.client.SendAsync(new TnUseAbility(world.clientTickId, position, target, abilityValue));
    }

    private void ColorFromTalisman(WorldEffect effect)
    {
        if (!SkillTreeFunctions.IsEnabled || socketedTalisman.IsBlank) return;
        if (!(socketedTalisman.GetInfo() is EquipmentInfo equip)) return;
        TalismanEffect.ApplyAbilityAoeColor(effect, equip.talismanEffects, Mathf.Floor(rage));
    }

    private Color GetTalismanAbilityAoeColor(Color fallback)
    {
        if (!SkillTreeFunctions.IsEnabled || socketedTalisman.IsBlank) return fallback;
        if (!(socketedTalisman.GetInfo() is EquipmentInfo equip)) return fallback;
        if (!TalismanEffect.TryGetAbilityAoeColor(equip.talismanEffects, out var color, Mathf.Floor(rage))) return fallback;
        return color.ToUnityColor();
    }

    private static StatType[] statTypes = (StatType[])Enum.GetValues(typeof(StatType));

    public bool CanLevelUp()
    {
        if (!NetConstants.Use_Manual_Stat_Leveling) return false;
        if (GetLevel() >= NetConstants.Max_Level) return false;

        foreach (var type in statTypes)
            if (CanLevelUp(type))
                return true;
        return false;
    }

    private bool CanLevelUp(StatType type)
    {
        var charInfo = (TitanCore.Data.Entities.CharacterInfo)info;
        if (GetStatBase(type) >= charInfo.stats[type].maxValue) return false;

        var statForCost = type == StatType.MaxHealth ? GetStatBase(type) / 10 - 5 : GetStatBase(type);
        var cost = StatFunctions.GetLevelUpCost(charInfo, type, statForCost, 1);
        return cost > 0 && cost <= fullSouls;
    }

    public bool CanSpendSkillTreePoint()
    {
        if (!SkillTreeFunctions.IsUnlocked(GetLevel())) return false;
        if (SkillTreeFunctions.GetSpentTotal(skillTreeRanks) >= SkillTreeFunctions.Point_Cap) return false;

        int cheapest = int.MaxValue;
        for (int i = 0; i < SkillTreeFunctions.Node_Count; i++)
        {
            int rank = SkillTreeFunctions.GetSpentRank(skillTreeRanks, (SkillTreeNode)i);
            if (rank >= SkillTreeFunctions.Max_Spent_Rank) continue;
            int cost = SkillTreeFunctions.GetRankCost(rank + 1);
            if (cost < cheapest)
                cheapest = cost;
        }
        return cheapest != int.MaxValue && cheapest <= fullSouls;
    }

    public void AddRage(float amount = 1, bool applyRageGainBonus = true)
    {
        if (HasStatusEffect(StatusEffect.Mundane)) return;
        if (applyRageGainBonus)
            amount = StatFunctions.ApplyRageGainBonus(amount, GetAlternateStatIncrease(AlternateStatType.RageGain));
        rage = Math.Min(rage + amount, 100f);
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

    public void ApplyClientAbilityHitModifiers(Entity enemy, ref DamageResult result)
    {
        if ((ClassType)info.id == ClassType.Nomad && IsNomadMarked(enemy))
        {
            float wrath = 0f;
            if (SkillTreeFunctions.IsEnabled)
                wrath = BuildClientAbilityModifiers().markedDamagePct;
            result.damage = AbilityFunctions.Nomad.ScaleMarkedDamage(result.damage, wrath);
        }

        int cleave = ConsumeWarriorCleaveOutgoing(world.clientTime);
        if (cleave > 0)
            result.damage += enemy.GetDamageTaken(cleave);
    }

    private int ConsumeWarriorCleaveOutgoing(uint time)
    {
        if ((ClassType)info.id != ClassType.Warrior) return 0;
        if (time > warriorAbilityEndTime) return 0;
        if (time < warriorNextPulseTime) return 0;

        var mods = SkillTreeFunctions.IsEnabled ? BuildClientAbilityModifiers() : AbilityModifierSnapshot.Empty;
        int lockout = mods.pulseLockoutMs > 0 ? mods.pulseLockoutMs : SkillTreeFunctions.Base_Pulse_Lockout_Ms;
        warriorNextPulseTime = time + (uint)lockout;
        if (mods.weaponDamagePct <= 0) return 0;

        var held = GetItem(0);
        if (held.IsBlank || !(held.GetInfo() is WeaponInfo weaponInfo) || weaponInfo.projectiles == null || weaponInfo.projectiles.Length == 0)
            return 0;

        WeaponFunctions.GetProjectileDamage(weaponInfo.slotType, weaponInfo.projectiles[0], out var minDamage, out var maxDamage);
        return AbilityFunctions.Warrior.GetCleaveOutgoing(
            minDamage,
            maxDamage,
            GetStatFunctional(StatType.Attack),
            HasStatusEffect(StatusEffect.Damaging),
            mods.weaponDamagePct);
    }

    public void ApplyNomadMarkedHitRage(Entity enemy, uint projectileStartTime)
    {
        if ((ClassType)info.id != ClassType.Nomad) return;
        if (enemy == null || HasStatusEffect(StatusEffect.Mundane)) return;
        if (!IsNomadMarked(enemy)) return;

        var mods = SkillTreeFunctions.IsEnabled ? BuildClientAbilityModifiers() : AbilityModifierSnapshot.Empty;
        if (mods.markedRage > 0)
            AddRage(mods.markedRage, false);

        if (!SkillTreeFunctions.IsEnabled || mods.talismanEffects == null) return;
        for (int i = 0; i < mods.talismanEffects.Length; i++)
        {
            var effect = mods.talismanEffects[i];
            if (effect.trigger != TalismanTrigger.HitMarked) continue;
            if (effect.rageGain <= 0) continue;
            if (!TalismanEffect.MeetsRageThreshold(abilityActivationRage, effect)) continue;
            if (projectileStartTime != 0)
            {
                long key = ((long)i << 32) ^ enemy.gameId;
                if (hitMarkedTalismanShots.TryGetValue(key, out var lastShot) && lastShot == projectileStartTime)
                    continue;
                hitMarkedTalismanShots[key] = projectileStartTime;
            }
            AddRage(effect.rageGain, false);
        }
    }

    private bool IsNomadMarked(Entity enemy)
    {
        if (enemy.HasStatusEffect(StatusEffect.Marked)) return true;

        float radius = 1f;
        if (SkillTreeFunctions.IsEnabled)
            radius += BuildClientAbilityModifiers().markRadiusBonus;
        for (int i = 0; i < NomadCharm.ActiveCharms.Count; i++)
        {
            var charm = NomadCharm.ActiveCharms[i];
            if (charm == null || charm.world == null) continue;
            if (Vector2.Distance(charm.Position, enemy.Position) <= radius + enemy.radius * enemy.size)
                return true;
        }
        return false;
    }

    private const float Emote_Cooldown = 0;

    public void UseEmote(EmoteType type)
    {
        var seconds = (float)(DateTime.Now - lastEmote).TotalSeconds;
        if (seconds < Emote_Cooldown)
        {
            world.GameChat($"Emote available in {Mathf.RoundToInt((Emote_Cooldown - seconds) * 10) / 10f} seconds.", ChatType.Error);
            return;
        }
        lastEmote = DateTime.Now;
        world.gameManager.client.SendAsync(new TnEmote(type));
    }

    public int GetStatLock(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth:
                return lockedMaxHealth;
            case StatType.Speed:
                return lockedSpeed;
            case StatType.Attack:
                return lockedAttack;
            case StatType.Defense:
                return lockedDefense;
            case StatType.Vigor:
                return lockedVigor;
        }
        return 0;
    }

    public void ApplySkillTreeState(uint packedRanks, Item talisman)
    {
        skillTreeRanks = packedRanks;
        socketedTalisman = talisman;
        RaiseInventoryUpdated();
    }

    public AbilityModifierSnapshot BuildClientAbilityModifiers()
    {
        if (!SkillTreeFunctions.IsUnlocked(GetLevel()))
            return AbilityModifierSnapshot.Empty;
        var equips = new Item[4];
        for (int i = 0; i < 4; i++)
            equips[i] = GetItem(i);
        return SkillTreeFunctions.BuildSnapshot((ClassType)info.id, skillTreeRanks, equips, socketedTalisman);
    }

    private int GetLancerAbilityRageCost()
    {
        var mods = SkillTreeFunctions.IsEnabled ? BuildClientAbilityModifiers() : AbilityModifierSnapshot.Empty;
        return AbilityFunctions.RageSpend.GetLancerRageCost(mods);
    }

    private void SpendDumpAbilityRage()
    {
        var mods = SkillTreeFunctions.IsEnabled ? BuildClientAbilityModifiers() : AbilityModifierSnapshot.Empty;
        rage = AbilityFunctions.RageSpend.SpendDumpRage(rage, mods, out _);
    }

    private void SpendFixedAbilityRage(int cost)
    {
        rage = AbilityFunctions.RageSpend.SpendFixedCost(rage, cost);
    }

    private bool HasEnoughRageForAbility()
    {
        int rageIntegral = Mathf.FloorToInt(rage);
        if (rageIntegral <= 0) return false;

        switch ((ClassType)info.id)
        {
            case ClassType.Lancer:
                return rageIntegral >= GetLancerAbilityRageCost();
            case ClassType.Minister:
                return rageIntegral >= AbilityFunctions.Minister.GetRageCost(rageIntegral);
            case ClassType.Nomad:
                return rageIntegral >= AbilityFunctions.Nomad.Ability_Cost;
            default:
                return true;
        }
    }

    public int GetGearTalentRank(SkillTreeNode node)
    {
        var gear = new int[SkillTreeFunctions.Node_Count];
        for (int i = 0; i < 4; i++)
            SkillTreeFunctions.AddGearRanks(GetItem(i), (ClassType)info.id, gear);
        return gear[(int)node];
    }
}