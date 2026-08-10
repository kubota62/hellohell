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

        return math.abs(spawnOrdinal) % 2 == 0
            ? EliteAffixKind.Swift
            : EliteAffixKind.Juggernaut;
    }

    public static float ResolveHealthMultiplier(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Juggernaut
            ? 1.6f
            : kind == EliteAffixKind.Swift ? 0.8f : 1f;
    }

    public static float ResolveSpeedMultiplier(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Swift
            ? 1.4f
            : kind == EliteAffixKind.Juggernaut ? 0.72f : 1f;
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
            : kind == EliteAffixKind.Swift ? 1.15f : 1f;
    }

    public static float4 ResolveColor(EliteAffixKind kind)
    {
        return kind == EliteAffixKind.Swift
            ? new float4(0.06f, 0.72f, 1f, 1f)
            : kind == EliteAffixKind.Juggernaut
                ? new float4(1f, 0.58f, 0.04f, 1f)
                : new float4(1f, 0.22f, 0.04f, 1f);
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
