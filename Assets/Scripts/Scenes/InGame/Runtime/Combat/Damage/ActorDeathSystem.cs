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
        var rewardLookup = SystemAPI.GetComponentLookup<EnemyReward>(true);
        var enemyTypeLookup = SystemAPI.GetComponentLookup<EnemyTypeId>(true);

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

            if (rewardLookup.HasComponent(entity))
            {
                var reward = rewardLookup[entity];
                var enemyTypeId = enemyTypeLookup.HasComponent(entity)
                    ? enemyTypeLookup[entity].Value
                    : 0;
                var rewardEventEntity = ecb.CreateEntity();
                ecb.AddComponent(rewardEventEntity, new EnemyRewardEvent
                {
                    EnemyTypeId = enemyTypeId,
                    Experience = reward.Experience,
                    Score = reward.Score,
                });
            }

            ecb.DestroyEntity(entity);
        }
    }
}
