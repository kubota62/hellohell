using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>敵撃破地点から放射し、短時間で縮小・消失する粒子。</summary>
public struct DeathBurstParticle : IComponentData
{
    public float3 Velocity;
    public float4 StartColor;
    public float StartScale;
    public float Elapsed;
    public float Lifetime;
}

/// <summary>敵ランクに応じた撃破粒子を既存のProjectile Prefabから生成する。</summary>
public static class DeathBurstSpawnUtility
{
    public static int ResolveParticleCount(
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        return isFinalBoss ? 18 : isChampion ? 12 : isElite ? 7 : 3;
    }

    public static float4 ResolveColor(
        float4 actorColor,
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        if (isFinalBoss)
        {
            return new float4(1f, 0.12f, 0.72f, 1f);
        }

        if (isChampion)
        {
            return new float4(1f, 0.68f, 0.08f, 1f);
        }

        if (isElite)
        {
            return new float4(1f, 0.3f, 0.06f, 1f);
        }

        var safeColor = math.saturate(actorColor);
        safeColor.xyz = math.lerp(safeColor.xyz, new float3(1f), 0.28f);
        safeColor.w = 1f;
        return safeColor;
    }

    public static void Spawn(
        EntityCommandBuffer ecb,
        Entity projectilePrefab,
        float3 position,
        float4 actorColor,
        int seed,
        int particleCount,
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        if (projectilePrefab == Entity.Null || particleCount <= 0)
        {
            return;
        }

        var color = ResolveColor(
            actorColor,
            isElite,
            isChampion,
            isFinalBoss);
        var rankScale = isFinalBoss
            ? 1.55f
            : isChampion
                ? 1.3f
                : isElite ? 1.1f : 0.85f;
        var lifetime = isFinalBoss
            ? 0.72f
            : isChampion
                ? 0.58f
                : isElite ? 0.46f : 0.34f;
        for (var i = 0; i < particleCount; i++)
        {
            var angle = (seed * 0.6180339f + i) * 2.3999632f;
            math.sincos(angle, out var sin, out var cos);
            var speed = rankScale * (4.2f + i % 3 * 0.7f);
            var velocity = new float3(
                cos * speed,
                2.6f + i % 4 * 0.45f,
                sin * speed);
            var startScale = rankScale * (0.18f + i % 2 * 0.05f);
            var particle = ecb.Instantiate(projectilePrefab);
            ecb.SetComponent(
                particle,
                LocalTransform.FromPositionRotationScale(
                    position + new float3(0f, 0.55f, 0f),
                    quaternion.identity,
                    startScale));
            ecb.SetComponent(particle, new URPMaterialPropertyBaseColor
            {
                Value = color,
            });
            ecb.AddComponent(particle, new DeathBurstParticle
            {
                Velocity = velocity,
                StartColor = color,
                StartScale = startScale,
                Lifetime = lifetime,
            });
            ecb.RemoveComponent<ProjectileMotion>(particle);
            ecb.RemoveComponent<Projectile>(particle);
            ecb.RemoveComponent<Lifetime>(particle);
            ecb.RemoveComponent<GameplayActive>(particle);
        }
    }
}

[BurstCompile]
[UpdateAfter(typeof(ActorDeathSystem))]
public partial struct DeathBurstAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DeathBurstParticle>();
        state.RequireForUpdate<
            EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<
                EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (particle, transform, color, entity) in
                 SystemAPI.Query<
                         RefRW<DeathBurstParticle>,
                         RefRW<LocalTransform>,
                         RefRW<URPMaterialPropertyBaseColor>>()
                     .WithEntityAccess())
        {
            ref var particleValue = ref particle.ValueRW;
            ref var transformValue = ref transform.ValueRW;
            ref var colorValue = ref color.ValueRW;
            if (Advance(
                    ref particleValue,
                    ref transformValue,
                    ref colorValue,
                    deltaTime))
            {
                ecb.DestroyEntity(entity);
            }
        }
    }

    public static bool Advance(
        ref DeathBurstParticle particle,
        ref LocalTransform transform,
        ref URPMaterialPropertyBaseColor color,
        float deltaTime)
    {
        var safeDeltaTime = math.max(0f, deltaTime);
        particle.Elapsed += safeDeltaTime;
        if (particle.Elapsed >= particle.Lifetime)
        {
            return true;
        }

        var progress = math.saturate(
            particle.Elapsed /
            math.max(0.01f, particle.Lifetime));
        var velocity = particle.Velocity;
        transform.Position += velocity * safeDeltaTime;
        transform.Scale = particle.StartScale *
            math.lerp(1f, 0.12f, progress);
        particle.Velocity = new float3(
            velocity.x * math.max(0f, 1f - safeDeltaTime * 3.2f),
            velocity.y - safeDeltaTime * 11f,
            velocity.z * math.max(0f, 1f - safeDeltaTime * 3.2f));

        var nextColor = particle.StartColor;
        nextColor.w = 1f - progress * progress;
        color.Value = nextColor;
        return false;
    }
}
