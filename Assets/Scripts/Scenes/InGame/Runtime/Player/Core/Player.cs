using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// <summary>
/// Player/Core の共通タグ。
/// 操作対象の ActorBody を識別する。
/// 敵移動システムはこのタグを持つエンティティを対象外にする。
/// </summary>
public struct Player : IComponentData
{
}

/// <summary>
/// HPが尽き、操作と攻撃を停止したプレイヤー。
/// </summary>
public struct PlayerDefeated : IComponentData
{
}

public struct PlayerRevivalGrace : IComponentData
{
    public float RemainingSeconds;
}

/// <summary>
/// 通常被弾後の短い無敵時間。同時多段ヒットによる瞬間的なHP消失を防ぐ。
/// </summary>
public struct PlayerDamageGrace : IComponentData
{
    public float RemainingSeconds;
    public float DurationSeconds;
    public float4 OriginalColor;
    public byte HasOriginalColor;
}

public struct PlayerRevivedEvent : IComponentData
{
    public int RestoredHealth;
    public int ChargesRemaining;
}

[UpdateBefore(typeof(ActorDamageEventSystem))]
public partial struct PlayerRevivalGraceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 ||
             runState.IsChoosingUpgrade != 0);
        var deltaTime = isPaused ? 0f : SystemAPI.Time.DeltaTime;
        foreach (var (grace, entity) in
                 SystemAPI.Query<RefRW<PlayerRevivalGrace>>()
                     .WithEntityAccess())
        {
            grace.ValueRW.RemainingSeconds -= deltaTime;
            if (grace.ValueRO.RemainingSeconds <= 0f)
            {
                ecb.RemoveComponent<PlayerRevivalGrace>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[UpdateBefore(typeof(ActorDamageEventSystem))]
public partial struct PlayerDamageGraceSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 ||
             runState.IsChoosingUpgrade != 0);
        var deltaTime = isPaused ? 0f : SystemAPI.Time.DeltaTime;
        foreach (var (grace, entity) in
                 SystemAPI.Query<RefRW<PlayerDamageGrace>>()
                     .WithEntityAccess())
        {
            grace.ValueRW.RemainingSeconds -= deltaTime;
            if (grace.ValueRO.RemainingSeconds <= 0f)
            {
                if (grace.ValueRO.HasOriginalColor != 0 &&
                    state.EntityManager.HasComponent<
                        URPMaterialPropertyBaseColor>(entity))
                {
                    state.EntityManager.SetComponentData(
                        entity,
                        new URPMaterialPropertyBaseColor
                        {
                            Value = grace.ValueRO.OriginalColor,
                        });
                }
                ecb.RemoveComponent<PlayerDamageGrace>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>通常被弾無敵中を青白い脈動で知らせる。</summary>
[BurstCompile]
[UpdateAfter(typeof(HitFlashSystem))]
public partial struct PlayerDamageGraceVisualSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerDamageGrace>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (grace, color) in
                 SystemAPI.Query<
                         RefRO<PlayerDamageGrace>,
                         RefRW<URPMaterialPropertyBaseColor>>()
                     .WithAll<Player>()
                     .WithNone<HitFlash, PlayerDefeated>())
        {
            if (grace.ValueRO.HasOriginalColor == 0)
            {
                continue;
            }

            color.ValueRW.Value = EvaluateGraceColor(grace.ValueRO);
        }
    }

    public static float4 EvaluateGraceColor(in PlayerDamageGrace grace)
    {
        var duration = math.max(0.01f, grace.DurationSeconds);
        var elapsed = math.max(
            0f,
            duration - grace.RemainingSeconds);
        var pulse = 0.5f + 0.5f * math.sin(elapsed * 26f);
        var shieldColor = new float4(0.2f, 0.72f, 1f, 1f);
        var blend = math.lerp(0.18f, 0.5f, pulse);
        var result = math.lerp(
            grace.OriginalColor,
            shieldColor,
            blend);
        result.w = grace.OriginalColor.w;
        return result;
    }
}

/// <summary>
/// プレイヤー死亡をラン終了状態へ変換する。
/// </summary>
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct PlayerDefeatSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<RunState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var runState = SystemAPI.GetSingleton<RunState>();
        if (runState.IsGameOver != 0)
        {
            return;
        }

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var defeated = false;
        foreach (var (health, skillStats, playerEntity) in
                 SystemAPI.Query<RefRW<Health>, RefRW<PlayerSkillStats>>()
                     .WithAll<Player>()
                     .WithNone<PlayerDefeated>()
                     .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
            {
                continue;
            }

            if (TryConsumeSecondWind(
                    ref skillStats.ValueRW,
                    ref health.ValueRW))
            {
                ecb.AddComponent(playerEntity, new PlayerRevivalGrace
                {
                    RemainingSeconds = 2f,
                });
                var eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new PlayerRevivedEvent
                {
                    RestoredHealth = health.ValueRO.Current,
                    ChargesRemaining =
                        skillStats.ValueRO.SecondWindChargesRemaining,
                });
                continue;
            }

            ecb.AddComponent<PlayerDefeated>(playerEntity);
            defeated = true;
        }

        if (defeated)
        {
            runState.IsGameOver = 1;
            SystemAPI.SetSingleton(runState);

            if (SystemAPI.TryGetSingletonRW<PlayerInput>(out var input))
            {
                input.ValueRW.Movement = default;
                input.ValueRW.IsFire = false;
                input.ValueRW.HasAimPosition = false;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static bool TryConsumeSecondWind(
        ref PlayerSkillStats stats,
        ref Health health)
    {
        if (health.Current > 0 ||
            health.Max <= 0 ||
            stats.SecondWindChargesRemaining <= 0)
        {
            return false;
        }

        var restoreFraction = Unity.Mathematics.math.clamp(
            stats.RevivalHealthFraction,
            0.1f,
            1f);
        health.Current = Unity.Mathematics.math.max(
            1,
            (int)Unity.Mathematics.math.round(
                health.Max * restoreFraction));
        stats.SecondWindChargesRemaining--;
        return true;
    }
}
