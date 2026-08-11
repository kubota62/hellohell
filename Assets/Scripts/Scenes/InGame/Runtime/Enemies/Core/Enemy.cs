using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Enemies/Core の共通タグ。
/// エンティティを Enemy として扱うために付与する。
/// Team は敵対関係、Enemy は敵用システムで動く Actor かどうかを表す。
/// </summary>
public struct Enemy : IComponentData
{
}

/// <summary>
/// 敵が出現した時点の脅威度に応じた攻撃威力倍率。
/// 生存中に威力が突然変化しないよう、スポーン時に固定する。
/// </summary>
public struct EnemyDamageMultiplier : IComponentData
{
    public float Value;
}

public static class EnemyDamageScaling
{
    const float MaximumTimeBonus = 0.75f;
    const float SecondsToMaximumBonus = 600f;

    public static float Resolve(
        float elapsedSeconds,
        bool isChampion,
        bool isFinalBoss,
        EliteAffixKind eliteAffix)
    {
        var timeBonus = math.min(
            MaximumTimeBonus,
            math.max(0f, elapsedSeconds) /
            SecondsToMaximumBonus * MaximumTimeBonus);
        var roleScale = isFinalBoss
            ? 0.45f
            : isChampion ? 0.65f : 1f;
        var affixMultiplier = eliteAffix == EliteAffixKind.Swift
            ? 1.15f
            : 1f;
        return (1f + timeBonus * roleScale) * affixMultiplier;
    }

    public static int Apply(int baseDamage, float multiplier)
    {
        return math.max(
            1,
            (int)math.round(
                math.max(1, baseDamage) *
                math.max(1f, multiplier)));
    }
}

/// <summary>
/// 通常敵より高い能力と報酬を持つ、ラン中の節目となる強敵。
/// </summary>
public struct EliteEnemy : IComponentData
{
}

public enum EliteAffixKind : byte
{
    None,
    Swift,
    Juggernaut,
    Volatile,
    Warded,
}

/// <summary>
/// 通常エリートへ付与する戦闘特性。チャンピオンと最終ボスは
/// 固有の能力曲線を持つため、この特性の対象外とする。
/// </summary>
public struct EliteAffix : IComponentData
{
    public EliteAffixKind Kind;
}

public static class EliteAffixUtility
{
    public static EliteAffixKind ResolveForSpawn(
        int spawnOrdinal,
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        if (!isElite || isChampion || isFinalBoss)
        {
            return EliteAffixKind.None;
        }

        return (math.abs(spawnOrdinal) % 4) switch
        {
            0 => EliteAffixKind.Swift,
            1 => EliteAffixKind.Juggernaut,
            2 => EliteAffixKind.Volatile,
            _ => EliteAffixKind.Warded,
        };
    }

    public static float ResolveHealthMultiplier(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Juggernaut
            ? 1.6f
            : kind == EliteAffixKind.Swift
                ? 0.8f
                : kind == EliteAffixKind.Volatile
                    ? 1.1f
                    : kind == EliteAffixKind.Warded ? 0.95f : 1f;
    }

    public static float ResolveSpeedMultiplier(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Swift
            ? 1.4f
            : kind == EliteAffixKind.Juggernaut
                ? 0.72f
                : kind == EliteAffixKind.Volatile ? 0.95f : 1f;
    }

    public static float ResolveScaleMultiplier(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Juggernaut
            ? 1.22f
            : kind == EliteAffixKind.Swift ? 0.92f : 1f;
    }

    public static float ResolveRewardMultiplier(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Juggernaut
            ? 1.4f
            : kind == EliteAffixKind.Swift
                ? 1.15f
                : kind == EliteAffixKind.Volatile
                    ? 1.3f
                    : kind == EliteAffixKind.Warded ? 1.35f : 1f;
    }

    public static float4 ResolveColor(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Swift
            ? new float4(0.06f, 0.72f, 1f, 1f)
            : kind == EliteAffixKind.Juggernaut
                ? new float4(1f, 0.58f, 0.04f, 1f)
                : kind == EliteAffixKind.Volatile
                    ? new float4(1f, 0.04f, 0.32f, 1f)
                    : kind == EliteAffixKind.Warded
                        ? new float4(0.16f, 0.42f, 1f, 1f)
                    : new float4(1f, 0.22f, 0.04f, 1f);
    }
}

/// <summary>結界エリートが持つ、体力より先にダメージを受ける再充填式の防壁。</summary>
public struct EliteBarrier : IComponentData
{
    public float Current;
    public float Maximum;
    public float RechargeDelayRemaining;
}

/// <summary>結界を破壊した瞬間をHUDへ通知する一度きりのイベント。</summary>
public struct EliteBarrierBrokenEvent : IComponentData
{
    public Entity Enemy;
}

/// <summary>結界破壊後、エリートの移動と攻撃を一時的に止めるひるみ状態。</summary>
public struct EliteBarrierBreakStagger : IComponentData
{
    public float RemainingSeconds;
}

public static class EliteBarrierBreakStaggerUtility
{
    public const float DurationSeconds = 1.25f;

    public static EliteBarrierBreakStagger Begin(
        EliteBarrierBreakStagger current)
    {
        current.RemainingSeconds = math.max(
            math.max(0f, current.RemainingSeconds),
            DurationSeconds);
        return current;
    }

    public static EliteBarrierBreakStagger Advance(
        EliteBarrierBreakStagger current,
        float deltaTime)
    {
        current.RemainingSeconds = math.max(
            0f,
            current.RemainingSeconds - math.max(0f, deltaTime));
        return current;
    }

    public static bool IsActive(EliteBarrierBreakStagger current)
    {
        return current.RemainingSeconds > 0f;
    }

    public static void AdvanceAndSuppressMovement(
        ref EliteBarrierBreakStagger current,
        ref MoveIntent moveIntent,
        float deltaTime)
    {
        current = Advance(current, deltaTime);
        if (!IsActive(current))
        {
            return;
        }

        moveIntent.Direction = float3.zero;
        moveIntent.Magnitude = 0f;
    }
}

public static class EliteBarrierUtility
{
    public const float HealthFraction = 0.35f;
    public const float RechargeDelaySeconds = 4f;
    public const float RechargeFractionPerSecond = 0.15f;

    public static EliteBarrier Create(int maximumHealth)
    {
        var maximum = math.max(
            1f,
            math.ceil(math.max(1, maximumHealth) * HealthFraction));
        return new EliteBarrier
        {
            Current = maximum,
            Maximum = maximum,
        };
    }

    public static int AbsorbDamage(
        ref EliteBarrier barrier,
        int incomingDamage,
        out int absorbedDamage)
    {
        var safeDamage = math.max(0, incomingDamage);
        if (safeDamage <= 0)
        {
            absorbedDamage = 0;
            return safeDamage;
        }

        barrier.RechargeDelayRemaining = RechargeDelaySeconds;
        if (barrier.Current <= 0f)
        {
            absorbedDamage = 0;
            return safeDamage;
        }

        absorbedDamage = math.min(
            safeDamage,
            math.max(0, (int)math.ceil(barrier.Current)));
        barrier.Current = math.max(
            0f,
            barrier.Current - absorbedDamage);
        return safeDamage - absorbedDamage;
    }

    public static EliteBarrier Advance(
        EliteBarrier barrier,
        float deltaTime,
        bool isPaused)
    {
        if (isPaused || barrier.Maximum <= 0f)
        {
            return barrier;
        }

        var remainingDelta = math.max(0f, deltaTime);
        var delayConsumed = math.min(
            math.max(0f, barrier.RechargeDelayRemaining),
            remainingDelta);
        barrier.RechargeDelayRemaining = math.max(
            0f,
            barrier.RechargeDelayRemaining - delayConsumed);
        remainingDelta -= delayConsumed;
        if (remainingDelta > 0f && barrier.Current < barrier.Maximum)
        {
            barrier.Current = math.min(
                barrier.Maximum,
                math.max(0f, barrier.Current) +
                    barrier.Maximum * RechargeFractionPerSecond *
                    remainingDelta);
        }

        return barrier;
    }

    public static float ResolveRatio(EliteBarrier barrier)
    {
        return barrier.Maximum > 0f
            ? math.saturate(barrier.Current / barrier.Maximum)
            : 0f;
    }
}

[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct EliteBarrierRechargeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EliteBarrier>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0);
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var barrier in
                 SystemAPI.Query<RefRW<EliteBarrier>>()
                     .WithAll<EliteEnemy, GameplayActive>())
        {
            barrier.ValueRW = EliteBarrierUtility.Advance(
                barrier.ValueRO,
                deltaTime,
                isPaused);
        }
    }
}

[BurstCompile]
[UpdateAfter(typeof(EnemyMovementSystem))]
[UpdateAfter(typeof(EnemySeparationSystem))]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EliteBarrierBreakStaggerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EliteBarrierBreakStagger>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (stagger, moveIntent) in
                 SystemAPI.Query<
                         RefRW<EliteBarrierBreakStagger>,
                         RefRW<MoveIntent>>()
                     .WithAll<EliteEnemy, GameplayActive>())
        {
            EliteBarrierBreakStaggerUtility.AdvanceAndSuppressMovement(
                ref stagger.ValueRW,
                ref moveIntent.ValueRW,
                deltaTime);
        }
    }
}

/// <summary>
/// 一定時間ごとに出現する中ボス級の強敵。
/// EliteEnemyも併せて持ち、既存の強敵向け処理へ参加する。
/// </summary>
public struct ChampionEnemy : IComponentData
{
}

public struct ChampionPhaseState : IComponentData
{
    public byte IsEnraged;
}

public struct ChampionEnragedEvent : IComponentData
{
}

/// <summary>
/// ラン終盤に一度だけ出現し、撃破が勝利条件になる最終ボス。
/// ChampionEnemyとEliteEnemyも併せて持つ。
/// </summary>
public struct FinalBossEnemy : IComponentData
{
}

public struct FinalBossPhaseState : IComponentData
{
    public byte IsEnraged;
}

public struct FinalBossEnragedEvent : IComponentData
{
}

[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct ChampionPhaseSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (health, phase, moveSpeed) in
                 SystemAPI.Query<
                         RefRO<Health>,
                         RefRW<ChampionPhaseState>,
                         RefRW<EnemyMoveSpeed>>()
                     .WithAll<ChampionEnemy, GameplayActive>()
                     .WithNone<FinalBossEnemy>())
        {
            if (phase.ValueRO.IsEnraged != 0 ||
                !IsEnraged(
                    health.ValueRO.Current,
                    health.ValueRO.Max))
            {
                continue;
            }

            phase.ValueRW.IsEnraged = 1;
            moveSpeed.ValueRW.Value = ResolveEnragedMoveSpeed(
                moveSpeed.ValueRO.Value);
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent<ChampionEnragedEvent>(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static bool IsEnraged(int currentHealth, int maxHealth)
    {
        return maxHealth > 0 &&
            currentHealth > 0 &&
            currentHealth <= maxHealth * 0.5f;
    }

    public static float ResolveEnragedMoveSpeed(float baseMoveSpeed)
    {
        return math.max(0.01f, baseMoveSpeed) * 1.3f;
    }
}

[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct FinalBossPhaseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(
            Unity.Collections.Allocator.Temp);
        foreach (var (health, phase) in
                 SystemAPI.Query<RefRO<Health>, RefRW<FinalBossPhaseState>>()
                     .WithAll<FinalBossEnemy, GameplayActive>())
        {
            if (phase.ValueRO.IsEnraged != 0 ||
                !IsEnraged(
                    health.ValueRO.Current,
                    health.ValueRO.Max))
            {
                continue;
            }

            phase.ValueRW.IsEnraged = 1;
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent<FinalBossEnragedEvent>(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static bool IsEnraged(int currentHealth, int maxHealth)
    {
        return maxHealth > 0 &&
            currentHealth > 0 &&
            currentHealth <= maxHealth / 2;
    }
}
