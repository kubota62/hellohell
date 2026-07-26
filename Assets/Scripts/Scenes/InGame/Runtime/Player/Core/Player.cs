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
        foreach (var (health, playerEntity) in
                 SystemAPI.Query<RefRO<Health>>()
                     .WithAll<Player>()
                     .WithNone<PlayerDefeated>()
                     .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
            {
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
}
