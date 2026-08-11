using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>エリートが落とす、短時間の戦闘強化を与える欠片。</summary>
public struct PowerShardPickup : IComponentData
{
    public float DurationSeconds;
    public float DamageMultiplier;
    public float CooldownMultiplier;
}

/// <summary>プレイヤーに適用中の力の欠片バフ。</summary>
public struct PowerShardBuff : IComponentData
{
    public float RemainingSeconds;
    public float DamageMultiplier;
    public float CooldownMultiplier;
}

/// <summary>力の欠片を拾った直後のHUD通知。</summary>
public struct PowerShardCollectedEvent : IComponentData
{
    public float DurationSeconds;
    public float DamageMultiplier;
    public float CooldownMultiplier;
}

public static class PowerShardBuffUtility
{
    public const float EliteDurationSeconds = 12f;
    public const float ChampionDurationSeconds = 18f;
    public const float EliteDamageMultiplier = 1.3f;
    public const float ChampionDamageMultiplier = 1.45f;
    public const float EliteCooldownMultiplier = 0.85f;
    public const float ChampionCooldownMultiplier = 0.78f;

    public static PowerShardPickup CreatePickup(bool isChampion)
    {
        return new PowerShardPickup
        {
            DurationSeconds = isChampion
                ? ChampionDurationSeconds
                : EliteDurationSeconds,
            DamageMultiplier = isChampion
                ? ChampionDamageMultiplier
                : EliteDamageMultiplier,
            CooldownMultiplier = isChampion
                ? ChampionCooldownMultiplier
                : EliteCooldownMultiplier,
        };
    }

    public static PowerShardBuff ApplyPickup(
        PowerShardBuff current,
        PowerShardPickup pickup)
    {
        var isCurrentActive = current.RemainingSeconds > 0f;
        var currentDamage = isCurrentActive
            ? ResolveDamageMultiplier(current)
            : 1f;
        var currentCooldown = isCurrentActive
            ? ResolveCooldownMultiplier(current)
            : 1f;
        return new PowerShardBuff
        {
            RemainingSeconds = math.max(
                math.max(0f, current.RemainingSeconds),
                math.max(0f, pickup.DurationSeconds)),
            DamageMultiplier = math.max(
                currentDamage,
                math.max(1f, pickup.DamageMultiplier)),
            CooldownMultiplier = math.min(
                currentCooldown,
                math.clamp(pickup.CooldownMultiplier, 0.1f, 1f)),
        };
    }

    public static PowerShardBuff Advance(
        PowerShardBuff buff,
        float deltaTime,
        bool isPaused)
    {
        if (!isPaused)
        {
            buff.RemainingSeconds = math.max(
                0f,
                buff.RemainingSeconds - math.max(0f, deltaTime));
        }

        return buff;
    }

    public static float ResolveDamageMultiplier(PowerShardBuff buff)
    {
        return buff.RemainingSeconds > 0f
            ? math.max(1f, buff.DamageMultiplier)
            : 1f;
    }

    public static float ResolveCooldownMultiplier(PowerShardBuff buff)
    {
        return buff.RemainingSeconds > 0f
            ? math.clamp(buff.CooldownMultiplier, 0.1f, 1f)
            : 1f;
    }
}

/// <summary>力の欠片を吸引し、接触時にプレイヤーの時限バフへ変換する。</summary>
[UpdateBefore(typeof(PowerShardBuffSystem))]
public partial struct PowerShardPickupSystem : ISystem
{
    const float BaseAttractionRadius = 2.75f;
    const float AttractionRadiusPerLevel = 0.18f;
    const float MaximumAttractionRadius = 14f;
    const float CollectRadius = 1.2f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<PowerShardPickup>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        if (!SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            return;
        }

        var playerPosition = SystemAPI.GetComponent<LocalTransform>(
            playerEntity).Position;
        playerPosition.y = 0.35f;
        var playerLevel = SystemAPI.HasComponent<PlayerProgress>(playerEntity)
            ? math.max(
                1,
                SystemAPI.GetComponent<PlayerProgress>(playerEntity).Level)
            : 1;
        var pickupRadiusAdd = SystemAPI.HasComponent<PlayerSkillStats>(
                playerEntity)
            ? math.max(
                0f,
                SystemAPI.GetComponent<PlayerSkillStats>(playerEntity)
                    .PickupRadiusAdd)
            : 0f;
        var attractionRadius = math.min(
            MaximumAttractionRadius,
            BaseAttractionRadius +
            (playerLevel - 1) * AttractionRadiusPerLevel +
            pickupRadiusAdd);
        var vacuumActive =
            SystemAPI.HasComponent<PickupVacuumPulse>(playerEntity) &&
            SystemAPI.GetComponent<PickupVacuumPulse>(playerEntity)
                .RemainingSeconds > 0f;
        attractionRadius = ExperiencePickupSystem.ResolveAttractionRadius(
            attractionRadius,
            vacuumActive);
        var attractionRadiusSq = attractionRadius * attractionRadius;
        var deltaTime = math.max(0f, SystemAPI.Time.DeltaTime);
        var collectedPickup = new PowerShardPickup();
        var hasCollectedPickup = false;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, pickup, entity) in
                 SystemAPI.Query<
                         RefRW<LocalTransform>,
                         RefRO<PowerShardPickup>>()
                     .WithEntityAccess())
        {
            var toPlayer = playerPosition - transform.ValueRO.Position;
            var distanceSq = math.lengthsq(toPlayer);
            if (distanceSq > attractionRadiusSq)
            {
                continue;
            }

            if (distanceSq <= CollectRadius * CollectRadius)
            {
                collectedPickup = MergePickups(
                    collectedPickup,
                    pickup.ValueRO,
                    hasCollectedPickup);
                hasCollectedPickup = true;
                ecb.DestroyEntity(entity);
                continue;
            }

            var distance = math.sqrt(distanceSq);
            var attraction = 1f - math.saturate(
                distance / attractionRadius);
            var speed = ExperiencePickupSystem.ResolveAttractionSpeed(
                attraction,
                vacuumActive);
            transform.ValueRW.Position +=
                math.normalizesafe(toPlayer) * speed * deltaTime;
            transform.ValueRW.Rotation = math.mul(
                quaternion.RotateY(deltaTime * 7f),
                transform.ValueRO.Rotation);
        }

        if (hasCollectedPickup)
        {
            var current = SystemAPI.HasComponent<PowerShardBuff>(playerEntity)
                ? SystemAPI.GetComponent<PowerShardBuff>(playerEntity)
                : new PowerShardBuff();
            var next = PowerShardBuffUtility.ApplyPickup(
                current,
                collectedPickup);
            if (SystemAPI.HasComponent<PowerShardBuff>(playerEntity))
            {
                ecb.SetComponent(playerEntity, next);
            }
            else
            {
                ecb.AddComponent(playerEntity, next);
            }

            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent(eventEntity, new PowerShardCollectedEvent
            {
                DurationSeconds = next.RemainingSeconds,
                DamageMultiplier = next.DamageMultiplier,
                CooldownMultiplier = next.CooldownMultiplier,
            });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static PowerShardPickup MergePickups(
        PowerShardPickup current,
        PowerShardPickup next,
        bool hasCurrent)
    {
        if (!hasCurrent)
        {
            return next;
        }

        return new PowerShardPickup
        {
            DurationSeconds = math.max(
                current.DurationSeconds,
                next.DurationSeconds),
            DamageMultiplier = math.max(
                current.DamageMultiplier,
                next.DamageMultiplier),
            CooldownMultiplier = math.min(
                current.CooldownMultiplier,
                next.CooldownMultiplier),
        };
    }
}

/// <summary>ゲーム進行中だけ力の欠片バフの残り時間を減らす。</summary>
[BurstCompile]
[UpdateBefore(typeof(AttackRequestSystem))]
public partial struct PowerShardBuffSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PowerShardBuff>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0);
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (buff, entity) in
                 SystemAPI.Query<RefRW<PowerShardBuff>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            var next = PowerShardBuffUtility.Advance(
                buff.ValueRO,
                deltaTime,
                isPaused);
            buff.ValueRW = next;
            if (next.RemainingSeconds <= 0f)
            {
                ecb.RemoveComponent<PowerShardBuff>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
