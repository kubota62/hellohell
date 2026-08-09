using Unity.Entities;

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
        var deltaTime = SystemAPI.Time.DeltaTime;
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
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (grace, entity) in
                 SystemAPI.Query<RefRW<PlayerDamageGrace>>()
                     .WithEntityAccess())
        {
            grace.ValueRW.RemainingSeconds -= deltaTime;
            if (grace.ValueRO.RemainingSeconds <= 0f)
            {
                ecb.RemoveComponent<PlayerDamageGrace>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
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
