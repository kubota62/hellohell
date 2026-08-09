using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// ActorBodyに積まれたDamageEventを集計し、Healthへ反映するシステム。
/// 表示や効果音などの演出はVfxRequestとして別Systemへ渡す。
/// </summary>
[BurstCompile]
public partial struct ActorDamageEventSystem : ISystem
{
    public const float PlayerHitImmunitySeconds = 0.35f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var hasPlayerSkillStats = SystemAPI.TryGetSingleton<PlayerSkillStats>(
            out var playerSkillStats);
        var attackerTransformLookup = SystemAPI.GetComponentLookup<
            LocalTransform>(true);
        var cameraShakeAmplitude = 0f;
        var cameraShakeDuration = 0f;
        var hitStopDuration = 0f;
        var hitStopTimeScale = 1f;

        foreach (var (transform, health, knockback, damageEventBuffer, actorEntity) in
                 SystemAPI.Query<
                         RefRO<LocalTransform>,
                         RefRW<Health>,
                         RefRW<EnemyKnockback>,
                         DynamicBuffer<DamageEvent>>()
                     .WithAll<ActorBody>()
                     .WithEntityAccess())
        {
            var isPlayerDamage =
                SystemAPI.HasComponent<Player>(actorEntity);
            if (SystemAPI.HasComponent<PlayerRevivalGrace>(actorEntity) ||
                isPlayerDamage &&
                SystemAPI.HasComponent<PlayerDamageGrace>(actorEntity))
            {
                damageEventBuffer.Clear();
                continue;
            }

            var pos = transform.ValueRO.Position + new float3(0f, 1f, 0f);
            var remainingHealth = health.ValueRO.Current;
            var acceptedPlayerHit = false;
            var accumulatedKnockback = float3.zero;
            var damageReduction =
                SystemAPI.HasComponent<Player>(actorEntity) &&
                SystemAPI.HasComponent<PlayerSkillStats>(actorEntity)
                    ? PlayerAutoSkillSystem.GetDamageReduction(
                        SystemAPI.GetComponent<PlayerSkillStats>(actorEntity))
                    : 0f;
            var executionDamageBonus =
                hasPlayerSkillStats &&
                SystemAPI.HasComponent<Enemy>(actorEntity)
                    ? PlayerAutoSkillSystem.GetExecutionDamageBonus(
                        playerSkillStats)
                    : 0f;
            var eliteDamageBonus =
                hasPlayerSkillStats &&
                SystemAPI.HasComponent<EliteEnemy>(actorEntity)
                    ? PlayerAutoSkillSystem.GetEliteDamageBonus(
                        playerSkillStats)
                    : 0f;

            // このフレームに溜まったダメージをまとめて減算し、各ヒットの表示だけ別リクエストへ逃がす。
            foreach (var damage in damageEventBuffer)
            {
                if (isPlayerDamage && acceptedPlayerHit)
                {
                    continue;
                }

                var eliteDamage = ResolveBonusDamage(
                    damage.Damage,
                    eliteDamageBonus);
                var executionDamage = ResolveExecutionDamage(
                    eliteDamage,
                    remainingHealth,
                    health.ValueRO.Max,
                    executionDamageBonus);
                var resolvedDamage = ResolveIncomingDamage(
                    executionDamage,
                    damageReduction);
                remainingHealth = math.max(0, remainingHealth - resolvedDamage);
                acceptedPlayerHit =
                    isPlayerDamage && resolvedDamage > 0;

                if (!isPlayerDamage &&
                    resolvedDamage > 0 &&
                    attackerTransformLookup.HasComponent(damage.Attacker))
                {
                    var direction = transform.ValueRO.Position -
                        attackerTransformLookup[damage.Attacker].Position;
                    direction.y = 0f;
                    direction = math.normalizesafe(direction);
                    accumulatedKnockback += direction * ResolveKnockbackImpulse(
                        resolvedDamage,
                        damage.IsCritical != 0,
                        SystemAPI.HasComponent<EliteEnemy>(actorEntity),
                        SystemAPI.HasComponent<ChampionEnemy>(actorEntity),
                        SystemAPI.HasComponent<FinalBossEnemy>(actorEntity));
                }

                var nextShakeAmplitude = ResolveCameraShakeAmplitude(
                    resolvedDamage,
                    damage.IsCritical != 0,
                    isPlayerDamage);
                if (nextShakeAmplitude > cameraShakeAmplitude)
                {
                    cameraShakeAmplitude = nextShakeAmplitude;
                    cameraShakeDuration = ResolveCameraShakeDuration(
                        damage.IsCritical != 0,
                        isPlayerDamage);
                }

                var nextHitStopDuration = CombatHitStopSystem.ResolveDuration(
                    resolvedDamage,
                    damage.IsCritical != 0,
                    isPlayerDamage);
                if (nextHitStopDuration > 0f)
                {
                    hitStopDuration = math.max(
                        hitStopDuration,
                        nextHitStopDuration);
                    hitStopTimeScale = math.min(
                        hitStopTimeScale,
                        CombatHitStopSystem.ResolveTimeScale(
                            resolvedDamage,
                            damage.IsCritical != 0,
                            isPlayerDamage));
                }

                var requestEntity = ecb.CreateEntity();
                ecb.AddComponent(requestEntity, new VfxRequest
                {
                    Kind = VfxRequestKind.DamageDigit,
                    IntValue = resolvedDamage,
                    Position = pos,
                    Target = actorEntity,
                    IsCritical = damage.IsCritical,
                    IsPlayerDamage = isPlayerDamage ? (byte)1 : (byte)0,
                });
            }

            if (acceptedPlayerHit)
            {
                ecb.AddComponent(actorEntity, new PlayerDamageGrace
                {
                    RemainingSeconds = PlayerHitImmunitySeconds,
                });
            }

            if (!isPlayerDamage &&
                math.lengthsq(accumulatedKnockback) > 0.0001f)
            {
                knockback.ValueRW.Velocity = ClampKnockbackVelocity(
                    knockback.ValueRO.Velocity + accumulatedKnockback);
            }

            health.ValueRW.Current = remainingHealth;
            damageEventBuffer.Clear();
        }

        if (cameraShakeAmplitude > 0f)
        {
            var shakeEntity = ecb.CreateEntity();
            ecb.AddComponent(shakeEntity, new CameraShakeRequest
            {
                Amplitude = cameraShakeAmplitude,
                DurationSeconds = cameraShakeDuration,
            });
        }

        if (hitStopDuration > 0f)
        {
            var hitStopEntity = ecb.CreateEntity();
            ecb.AddComponent(hitStopEntity, new CombatHitStopRequest
            {
                DurationSeconds = hitStopDuration,
                TimeScale = hitStopTimeScale,
            });
        }
    }

    public static float ResolveCameraShakeAmplitude(
        int damage,
        bool isCritical,
        bool isPlayerDamage)
    {
        if (damage <= 0)
        {
            return 0f;
        }

        if (isPlayerDamage)
        {
            return math.clamp(0.18f + damage * 0.005f, 0.18f, 0.32f);
        }

        return isCritical
            ? math.clamp(0.06f + damage * 0.002f, 0.06f, 0.14f)
            : 0f;
    }

    public static float ResolveCameraShakeDuration(
        bool isCritical,
        bool isPlayerDamage)
    {
        return isPlayerDamage ? 0.16f : isCritical ? 0.09f : 0f;
    }

    public static float ResolveKnockbackImpulse(
        int damage,
        bool isCritical,
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        if (damage <= 0)
        {
            return 0f;
        }

        var resistance = isFinalBoss
            ? 0.9f
            : isChampion
                ? 0.7f
                : isElite ? 0.45f : 0f;
        var impulse = 1.8f + math.sqrt(damage) * 0.18f;
        if (isCritical)
        {
            impulse *= 1.35f;
        }

        return impulse * (1f - resistance);
    }

    public static float3 ClampKnockbackVelocity(float3 velocity)
    {
        velocity.y = 0f;
        var speedSq = math.lengthsq(velocity);
        return speedSq > 100f
            ? math.normalizesafe(velocity) * 10f
            : velocity;
    }

    public static int ResolveIncomingDamage(int damage, float reduction)
    {
        if (damage <= 0)
        {
            return 0;
        }

        var safeReduction = math.clamp(reduction, 0f, 0.65f);
        return math.max(
            1,
            (int)math.round(damage * (1f - safeReduction)));
    }

    public static int ResolveBonusDamage(int damage, float damageBonus)
    {
        if (damage <= 0)
        {
            return 0;
        }

        var safeBonus = math.clamp(damageBonus, 0f, 2f);
        return math.max(
            1,
            (int)math.round(damage * (1f + safeBonus)));
    }

    public static int ResolveExecutionDamage(
        int damage,
        int currentHealth,
        int maxHealth,
        float damageBonus)
    {
        if (damage <= 0)
        {
            return 0;
        }

        if (maxHealth <= 0 ||
            math.clamp(currentHealth / (float)maxHealth, 0f, 1f) > 0.3f)
        {
            return damage;
        }

        var safeBonus = math.clamp(damageBonus, 0f, 1.5f);
        return math.max(
            1,
            (int)math.round(damage * (1f + safeBonus)));
    }
}

/// <summary>
/// ゲームロジックから演出層へ渡す、使い捨ての演出リクエスト。
/// ダメージ、死亡、攻撃発生などの見た目をロジック本体から分離するために使う。
/// </summary>
public struct VfxRequest : IComponentData
{
    public VfxRequestKind Kind;
    public int IntValue;
    public float3 Position;
    public Entity Target;
    public byte IsCritical;
    public byte IsPlayerDamage;
}

/// <summary>
/// VfxRequestSystemが処理できる演出の種類。
/// </summary>
public enum VfxRequestKind : byte
{
    DamageDigit = 1,
}

/// <summary>
/// VfxRequestを消費し、実際の演出エンティティ生成へ変換するシステム。
/// ロジック側は演出Prefabや表示方式を知らず、このSystemが演出の窓口になる。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct VfxRequestSystem : ISystem
{
    private EntityQuery requestQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        requestQuery = SystemAPI.QueryBuilder()
            .WithAll<VfxRequest>()
            .Build();
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<VfxRequest>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var colorLookup = SystemAPI.GetComponentLookup<
            URPMaterialPropertyBaseColor>(true);
        var hitFlashLookup = SystemAPI.GetComponentLookup<HitFlash>(true);
        var queuedHitFlashes = new NativeParallelHashMap<Entity, HitFlash>(
            math.max(1, requestQuery.CalculateEntityCount()),
            Allocator.Temp);

        foreach (var (request, entity) in
                 SystemAPI.Query<RefRO<VfxRequest>>()
                     .WithEntityAccess())
        {
            if (request.ValueRO.Kind == VfxRequestKind.DamageDigit)
            {
                DamageDigitSpawnUtility.SpawnDamageDigits(
                    ecb,
                    config.DamageDigitPrefab,
                    request.ValueRO.IntValue,
                    request.ValueRO.Position,
                    request.ValueRO.IsCritical != 0,
                    request.ValueRO.IsPlayerDamage != 0);
                CollectHitFlash(
                    state.EntityManager,
                    colorLookup,
                    hitFlashLookup,
                    ref queuedHitFlashes,
                    request.ValueRO);
            }

            ecb.DestroyEntity(entity);
        }

        foreach (var pair in queuedHitFlashes)
        {
            if (hitFlashLookup.HasComponent(pair.Key))
            {
                ecb.SetComponent(pair.Key, pair.Value);
            }
            else
            {
                ecb.AddComponent(pair.Key, pair.Value);
            }

            ecb.SetComponent(pair.Key, new URPMaterialPropertyBaseColor
            {
                Value = pair.Value.FlashColor,
            });
        }

        queuedHitFlashes.Dispose();
    }

    private static void CollectHitFlash(
        EntityManager entityManager,
        ComponentLookup<URPMaterialPropertyBaseColor> colorLookup,
        ComponentLookup<HitFlash> hitFlashLookup,
        ref NativeParallelHashMap<Entity, HitFlash> queuedHitFlashes,
        in VfxRequest request)
    {
        if (request.IntValue <= 0 ||
            request.Target == Entity.Null ||
            !entityManager.Exists(request.Target) ||
            !colorLookup.HasComponent(request.Target))
        {
            return;
        }

        var hasExistingFlash = hitFlashLookup.HasComponent(request.Target);
        var duration = ResolveHitFlashDuration(
            request.IsCritical != 0,
            request.IsPlayerDamage != 0);
        if (queuedHitFlashes.TryGetValue(
                request.Target,
                out var queuedFlash))
        {
            queuedFlash.RemainingSeconds = math.max(
                queuedFlash.RemainingSeconds,
                duration);
            queuedFlash.DurationSeconds = math.max(
                queuedFlash.DurationSeconds,
                duration);
            if (duration >= queuedFlash.DurationSeconds)
            {
                queuedFlash.FlashColor = ResolveHitFlashColor(
                    request.IsCritical != 0,
                    request.IsPlayerDamage != 0);
            }

            queuedHitFlashes[request.Target] = queuedFlash;
            return;
        }

        var originalColor = hasExistingFlash
            ? hitFlashLookup[request.Target].OriginalColor
            : colorLookup[request.Target].Value;
        var flash = new HitFlash
        {
            OriginalColor = originalColor,
            FlashColor = ResolveHitFlashColor(
                request.IsCritical != 0,
                request.IsPlayerDamage != 0),
            RemainingSeconds = duration,
            DurationSeconds = duration,
        };
        queuedHitFlashes.TryAdd(request.Target, flash);
    }

    public static float ResolveHitFlashDuration(
        bool isCritical,
        bool isPlayerDamage)
    {
        return isPlayerDamage ? 0.18f : isCritical ? 0.14f : 0.09f;
    }

    public static float4 ResolveHitFlashColor(
        bool isCritical,
        bool isPlayerDamage)
    {
        if (isPlayerDamage)
        {
            return new float4(1f, 0.16f, 0.12f, 1f);
        }

        return isCritical
            ? new float4(1f, 0.72f, 0.08f, 1f)
            : new float4(1f, 1f, 1f, 1f);
    }
}

/// <summary>
/// Actorの元色を保持し、被弾色から短時間で滑らかに復帰させる。
/// </summary>
public struct HitFlash : IComponentData
{
    public float4 OriginalColor;
    public float4 FlashColor;
    public float RemainingSeconds;
    public float DurationSeconds;
}

[BurstCompile]
[UpdateAfter(typeof(VfxRequestSystem))]
public partial struct HitFlashSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HitFlash>();
        state.RequireForUpdate<
            EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<
                EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (flash, color, entity) in
                 SystemAPI.Query<
                         RefRW<HitFlash>,
                         RefRW<URPMaterialPropertyBaseColor>>()
                     .WithEntityAccess())
        {
            flash.ValueRW.RemainingSeconds -= deltaTime;
            if (flash.ValueRO.RemainingSeconds <= 0f)
            {
                color.ValueRW.Value = flash.ValueRO.OriginalColor;
                ecb.RemoveComponent<HitFlash>(entity);
                continue;
            }

            var blend = math.saturate(
                flash.ValueRO.RemainingSeconds /
                math.max(0.01f, flash.ValueRO.DurationSeconds));
            color.ValueRW.Value = math.lerp(
                flash.ValueRO.OriginalColor,
                flash.ValueRO.FlashColor,
                blend);
        }
    }
}
