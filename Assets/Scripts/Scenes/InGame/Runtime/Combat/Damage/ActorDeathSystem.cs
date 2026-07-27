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
    private const int MaximumExperiencePickups = 500;
    private const int MaximumHealthPickups = 60;

    private EntityQuery experiencePickupQuery;
    private EntityQuery healthPickupQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        experiencePickupQuery = SystemAPI.QueryBuilder()
            .WithAll<ExperiencePickup>()
            .Build();
        healthPickupQuery = SystemAPI.QueryBuilder()
            .WithAll<HealthPickup>()
            .Build();
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var rewardLookup = SystemAPI.GetComponentLookup<EnemyReward>(true);
        var eliteLookup = SystemAPI.GetComponentLookup<EliteEnemy>(true);
        var championLookup = SystemAPI.GetComponentLookup<ChampionEnemy>(true);
        var config = SystemAPI.GetSingleton<Config>();
        var experiencePickupCount = experiencePickupQuery.CalculateEntityCount();
        var healthPickupCount = healthPickupQuery.CalculateEntityCount();

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
                var isElite = eliteLookup.HasComponent(entity);
                var isChampion = championLookup.HasComponent(entity);
                if (experiencePickupCount < MaximumExperiencePickups)
                {
                    CreateExperiencePickup(
                        ecb,
                        config.ProjectilePrefab,
                        transform.ValueRO.Position,
                        reward,
                        isChampion);
                    experiencePickupCount++;
                }
                else
                {
                    CreateRewardEvent(ecb, reward);
                }

                if (isChampion)
                {
                    CreateChampionDefeatedEvent(ecb, reward);
                }

                var dropsHealth = isElite ||
                    isChampion ||
                    entity.Index % 18 == 0;
                if (dropsHealth && healthPickupCount < MaximumHealthPickups)
                {
                    CreateHealthPickup(
                        ecb,
                        config.ProjectilePrefab,
                        transform.ValueRO.Position,
                        isElite,
                        isChampion);
                    healthPickupCount++;
                }
            }

            ecb.DestroyEntity(entity);
        }
    }

    private static void CreateHealthPickup(
        EntityCommandBuffer ecb,
        Entity projectilePrefab,
        float3 position,
        bool isElite,
        bool isChampion)
    {
        var healing = isChampion ? 40 : isElite ? 18 : 8;
        var pickupEntity = ecb.Instantiate(projectilePrefab);
        ecb.SetComponent(pickupEntity, LocalTransform.FromPositionRotationScale(
            position + new float3(0.45f, 0.35f, 0f),
            quaternion.identity,
            isChampion ? 0.75f : isElite ? 0.58f : 0.42f));
        ecb.SetComponent(pickupEntity, new URPMaterialPropertyBaseColor
        {
            Value = isChampion
                ? new float4(1f, 0.22f, 0.62f, 1f)
                : new float4(1f, 0.12f, 0.18f, 1f),
        });
        ecb.AddComponent(pickupEntity, new HealthPickup
        {
            Healing = healing,
        });
        ecb.RemoveComponent<ProjectileMotion>(pickupEntity);
        ecb.RemoveComponent<Projectile>(pickupEntity);
        ecb.RemoveComponent<Lifetime>(pickupEntity);
        ecb.RemoveComponent<GameplayActive>(pickupEntity);
    }

    private static void CreateExperiencePickup(
        EntityCommandBuffer ecb,
        Entity projectilePrefab,
        float3 position,
        EnemyReward reward,
        bool isChampion)
    {
        var pickupEntity = ecb.Instantiate(projectilePrefab);
        ecb.SetComponent(pickupEntity, LocalTransform.FromPositionRotationScale(
            position + new float3(0f, 0.35f, 0f),
            quaternion.identity,
            isChampion ? 0.85f : 0.45f));
        ecb.SetComponent(pickupEntity, new URPMaterialPropertyBaseColor
        {
            Value = isChampion
                ? new float4(1f, 0.68f, 0.08f, 1f)
                : new float4(0.2f, 1f, 0.35f, 1f),
        });
        ecb.AddComponent(pickupEntity, new ExperiencePickup
        {
            Experience = reward.Experience,
            Score = reward.Score,
        });
        if (isChampion)
        {
            ecb.AddComponent<ChampionRewardPickup>(pickupEntity);
        }
        ecb.RemoveComponent<ProjectileMotion>(pickupEntity);
        ecb.RemoveComponent<Projectile>(pickupEntity);
        ecb.RemoveComponent<Lifetime>(pickupEntity);
        ecb.RemoveComponent<GameplayActive>(pickupEntity);
    }

    private static void CreateRewardEvent(
        EntityCommandBuffer ecb,
        EnemyReward reward)
    {
        var rewardEntity = ecb.CreateEntity();
        ecb.AddComponent(rewardEntity, new EnemyRewardEvent
        {
            EnemyTypeId = 0,
            Experience = reward.Experience,
            Score = reward.Score,
        });
    }

    private static void CreateChampionDefeatedEvent(
        EntityCommandBuffer ecb,
        EnemyReward reward)
    {
        var eventEntity = ecb.CreateEntity();
        ecb.AddComponent(eventEntity, new ChampionDefeatedEvent
        {
            Experience = reward.Experience,
            Score = reward.Score,
        });
    }
}
