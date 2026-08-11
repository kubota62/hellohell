using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// 爆裂エリートの死亡地点に残る、回避可能な遅延爆発。
/// </summary>
public struct VolatileDeathHazard : IComponentData
{
    public float Elapsed;
    public float WindupSeconds;
    public float Radius;
    public int Damage;
}

public static class VolatileDeathHazardUtility
{
    public const float DefaultWindupSeconds = 0.85f;
    public const float DefaultRadius = 3.2f;
    public const int BaseDamage = 14;

    static readonly float4 WarningColor =
        new(1f, 0.025f, 0.12f, 1f);

    public static int ResolveDamage(float enemyDamageMultiplier)
    {
        return EnemyDamageScaling.Apply(BaseDamage, enemyDamageMultiplier);
    }

    public static bool IsInsideBlast(
        float3 hazardPosition,
        float3 targetPosition,
        float radius,
        float targetRadius)
    {
        var offset = targetPosition - hazardPosition;
        offset.y = 0f;
        var combinedRadius = math.max(0f, radius) +
            math.max(0f, targetRadius);
        return math.lengthsq(offset) <= combinedRadius * combinedRadius;
    }

    public static void EvaluateVisual(
        in VolatileDeathHazard hazard,
        out float scale,
        out float4 color)
    {
        var progress = math.saturate(
            hazard.Elapsed / math.max(0.01f, hazard.WindupSeconds));
        var pulse = 0.5f + 0.5f * math.sin(progress * math.PI * 10f);
        scale = math.max(0.2f, hazard.Radius * 2f) *
            math.lerp(0.18f, 1f, progress) *
            math.lerp(0.92f, 1.06f, pulse);
        color = math.lerp(
            WarningColor,
            new float4(1f, 0.78f, 0.12f, 1f),
            progress * 0.62f + pulse * 0.16f);
        color.w = math.lerp(0.3f, 0.82f, progress);
    }

    public static void Spawn(
        EntityManager entityManager,
        EntityCommandBuffer ecb,
        Entity visualPrefab,
        float3 position,
        int damage)
    {
        if (visualPrefab == Entity.Null)
        {
            return;
        }

        var hazard = new VolatileDeathHazard
        {
            WindupSeconds = DefaultWindupSeconds,
            Radius = DefaultRadius,
            Damage = math.max(1, damage),
        };
        EvaluateVisual(hazard, out var scale, out var color);

        var entity = ecb.Instantiate(visualPrefab);
        ecb.SetComponent(
            entity,
            LocalTransform.FromPositionRotationScale(
                position + new float3(0f, 0.08f, 0f),
                quaternion.identity,
                scale));
        ecb.SetComponent(entity, new URPMaterialPropertyBaseColor
        {
            Value = color,
        });
        var flatten = new PostTransformMatrix
        {
            Value = float4x4.Scale(new float3(1f, 0.035f, 1f)),
        };
        if (entityManager.HasComponent<PostTransformMatrix>(visualPrefab))
        {
            ecb.SetComponent(entity, flatten);
        }
        else
        {
            ecb.AddComponent(entity, flatten);
        }

        ecb.AddComponent(entity, hazard);
        ecb.RemoveComponent<ProjectileMotion>(entity);
        ecb.RemoveComponent<Projectile>(entity);
        ecb.RemoveComponent<Lifetime>(entity);
        ecb.RemoveComponent<GameplayActive>(entity);
        ecb.RemoveComponent<Hitbox>(entity);
        ecb.RemoveComponent<SpatialHashTarget>(entity);
        ecb.RemoveComponent<Team>(entity);
        ecb.RemoveComponent<PooledInstance>(entity);
    }
}

/// <summary>
/// 予兆を進め、完了時に範囲内のプレイヤーへ一度だけダメージを送る。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(ActorDamageEventSystem))]
public partial struct VolatileDeathHazardSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VolatileDeathHazard>();
        state.RequireForUpdate<
            EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0);
        var deltaTime = isPaused ? 0f : math.max(0f, SystemAPI.Time.DeltaTime);
        var hasPlayer =
            SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity) &&
            SystemAPI.HasBuffer<DamageEvent>(playerEntity) &&
            (!SystemAPI.HasComponent<Health>(playerEntity) ||
             SystemAPI.GetComponent<Health>(playerEntity).Current > 0);
        var playerPosition = hasPlayer
            ? SystemAPI.GetComponent<LocalTransform>(playerEntity).Position
            : float3.zero;
        var playerRadius = hasPlayer && SystemAPI.HasComponent<Hitbox>(playerEntity)
            ? SystemAPI.GetComponent<Hitbox>(playerEntity).Radius
            : 0f;
        var ecb = SystemAPI.GetSingleton<
                EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (hazard, transform, color, entity) in
                 SystemAPI.Query<
                         RefRW<VolatileDeathHazard>,
                         RefRW<LocalTransform>,
                         RefRW<URPMaterialPropertyBaseColor>>()
                     .WithEntityAccess())
        {
            hazard.ValueRW.Elapsed += deltaTime;
            VolatileDeathHazardUtility.EvaluateVisual(
                hazard.ValueRO,
                out var scale,
                out var nextColor);
            transform.ValueRW.Scale = scale;
            color.ValueRW.Value = nextColor;

            if (hazard.ValueRO.Elapsed < hazard.ValueRO.WindupSeconds)
            {
                continue;
            }

            if (hasPlayer && VolatileDeathHazardUtility.IsInsideBlast(
                    transform.ValueRO.Position,
                    playerPosition,
                    hazard.ValueRO.Radius,
                    playerRadius))
            {
                SystemAPI.GetBuffer<DamageEvent>(playerEntity).Add(
                    new DamageEvent
                    {
                        Damage = hazard.ValueRO.Damage,
                        Attacker = Entity.Null,
                    });
            }

            ecb.DestroyEntity(entity);
        }
    }
}
