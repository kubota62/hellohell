using Unity.Collections;
using Unity.Entities;

/// <summary>
/// EnemyRewardEventを一フレームだけ残して破棄するシステム。
/// 将来の経験値やスコア処理は通常のSimulation中にイベントを読み、このシステムがLateSimulationで掃除する。
/// </summary>
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct EnemyRewardEventCleanupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (_, entity) in
                 SystemAPI.Query<RefRO<EnemyRewardEvent>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
