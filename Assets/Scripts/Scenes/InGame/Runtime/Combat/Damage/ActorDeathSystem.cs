using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// Health が 0 以下になった ActorBody をゲーム世界から除去するシステム。
/// Player のゲームオーバー処理は未設計のため、現時点では非 Player だけを破棄する。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct ActorDeathSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var rewardLookup = SystemAPI.GetComponentLookup<EnemyReward>(true);
        var config = SystemAPI.GetSingleton<Config>();

        foreach (var (health, transform, entity) in
                 SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
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
                var pickupEntity = ecb.Instantiate(config.ProjectilePrefab);
                ecb.SetComponent(pickupEntity, LocalTransform.FromPositionRotationScale(
                    transform.ValueRO.Position + new float3(0f, 0.35f, 0f),
                    quaternion.identity,
                    0.45f));
                ecb.SetComponent(pickupEntity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(0.2f, 1f, 0.35f, 1f),
                });
                ecb.AddComponent(pickupEntity, new ExperiencePickup
                {
                    Experience = reward.Experience,
                    Score = reward.Score,
                });
                ecb.RemoveComponent<ProjectileMotion>(pickupEntity);
                ecb.RemoveComponent<Projectile>(pickupEntity);
                ecb.RemoveComponent<Lifetime>(pickupEntity);
                ecb.RemoveComponent<GameplayActive>(pickupEntity);
            }

            ecb.DestroyEntity(entity);
        }
    }
}
