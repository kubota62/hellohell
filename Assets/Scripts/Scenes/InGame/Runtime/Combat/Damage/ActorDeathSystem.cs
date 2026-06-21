using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Health が 0 以下になった ActorBody をゲーム世界から除去するシステム。
/// 将来プール運用へ移行する場合は、このシステムの DestroyEntity を無効化と再利用に差し替える。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct ActorDeathSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (health, entity) in
                 SystemAPI.Query<RefRO<Health>>()
                     .WithAll<ActorBody>()
                     .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
            {
                continue;
            }

            ecb.DestroyEntity(entity);
        }
    }
}
