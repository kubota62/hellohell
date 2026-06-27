using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Health が 0 以下になった ActorBody をゲーム世界から除去するシステム。
/// Player のゲームオーバー処理は未設計のため、現時点では非 Player だけを破棄する。
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
                     .WithNone<Player>()
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
