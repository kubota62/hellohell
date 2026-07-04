using Unity.Burst;
using Unity.Entities;

/// <summary>
/// EnemyRewardEventを一フレームだけ残して破棄するシステム。
/// 将来の経験値やスコア処理は通常のSimulation中にイベントを読み、このシステムがLateSimulationで掃除する。
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct EnemyRewardEventCleanupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (_, entity) in
                 SystemAPI.Query<RefRO<EnemyRewardEvent>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
    }
}
