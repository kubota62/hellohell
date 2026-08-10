using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>強敵の周囲を回転する、戦闘判定を持たないオーラ片。</summary>
public struct ThreatAuraSegment : IComponentData
{
    public Entity Owner;
    public float Angle;
    public float Radius;
    public float AngularSpeed;
    public float BaseScale;
    public float Phase;
    public float Elapsed;
    public float4 BaseColor;
}

public static class ThreatAuraSpawnUtility
{
    public static int ResolveSegmentCount(
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        return isFinalBoss ? 16 : isChampion ? 10 : isElite ? 6 : 0;
    }

    public static float ResolveRadius(
        float hitRadius,
        bool isChampion,
        bool isFinalBoss)
    {
        return math.max(0.5f, hitRadius) +
            (isFinalBoss ? 1.25f : isChampion ? 0.9f : 0.6f);
    }

    public static float4 ResolveColor(
        bool isChampion,
        bool isFinalBoss,
        EliteAffixKind eliteAffix = EliteAffixKind.None)
    {
        return isFinalBoss
            ? new float4(1f, 0.04f, 0.16f, 0.9f)
            : isChampion
                ? new float4(0.78f, 0.12f, 1f, 0.82f)
                : WithAlpha(
                    EliteAffixUtility.ResolveColor(eliteAffix),
                    0.76f);
    }

    public static ThreatAuraSegment CreateSegment(
        Entity owner,
        int index,
        int segmentCount,
        float radius,
        bool isChampion,
        bool isFinalBoss,
        EliteAffixKind eliteAffix = EliteAffixKind.None)
    {
        var safeCount = math.max(1, segmentCount);
        return new ThreatAuraSegment
        {
            Owner = owner,
            Angle = 2f * math.PI * index / safeCount,
            Radius = math.max(0.5f, radius),
            AngularSpeed = isFinalBoss
                ? -1.35f
                : isChampion ? 1.65f : 1.25f,
            BaseScale = isFinalBoss
                ? 0.34f
                : isChampion ? 0.27f : 0.21f,
            Phase = 2f * math.PI * index / safeCount,
            BaseColor = ResolveColor(
                isChampion,
                isFinalBoss,
                eliteAffix),
        };
    }

    public static int Spawn(
        EntityManager entityManager,
        Entity visualPrefab,
        Entity owner,
        bool isElite,
        bool isChampion,
        bool isFinalBoss,
        EliteAffixKind eliteAffix = EliteAffixKind.None)
    {
        var segmentCount = ResolveSegmentCount(
            isElite,
            isChampion,
            isFinalBoss);
        if (visualPrefab == Entity.Null ||
            segmentCount <= 0 ||
            !entityManager.Exists(owner) ||
            !entityManager.HasComponent<LocalTransform>(owner) ||
            !entityManager.HasComponent<Hitbox>(owner))
        {
            return 0;
        }

        var ownerPosition =
            entityManager.GetComponentData<LocalTransform>(owner).Position;
        var hitRadius = entityManager.GetComponentData<Hitbox>(owner).Radius;
        var radius = ResolveRadius(
            hitRadius,
            isChampion,
            isFinalBoss);
        for (var i = 0; i < segmentCount; i++)
        {
            var aura = CreateSegment(
                owner,
                i,
                segmentCount,
                radius,
                isChampion,
                isFinalBoss,
                eliteAffix);
            EvaluateVisual(
                aura,
                ownerPosition,
                out var position,
                out var scale,
                out var color);

            var segment = entityManager.Instantiate(visualPrefab);
            entityManager.SetComponentData(
                segment,
                LocalTransform.FromPositionRotationScale(
                    position,
                    quaternion.RotateY(-aura.Angle),
                    scale));
            SetOrAddColor(entityManager, segment, color);
            entityManager.AddComponentData(segment, aura);
            RemoveIfPresent<ProjectileMotion>(entityManager, segment);
            RemoveIfPresent<Projectile>(entityManager, segment);
            RemoveIfPresent<Lifetime>(entityManager, segment);
            RemoveIfPresent<GameplayActive>(entityManager, segment);
            RemoveIfPresent<Hitbox>(entityManager, segment);
            RemoveIfPresent<SpatialHashTarget>(entityManager, segment);
            RemoveIfPresent<Team>(entityManager, segment);
            RemoveIfPresent<PooledInstance>(entityManager, segment);
        }

        return segmentCount;
    }

    static float4 WithAlpha(float4 color, float alpha)
    {
        color.w = alpha;
        return color;
    }

    public static void EvaluateVisual(
        ThreatAuraSegment aura,
        float3 ownerPosition,
        out float3 position,
        out float scale,
        out float4 color)
    {
        EvaluateVisual(
            aura,
            ownerPosition,
            false,
            out position,
            out scale,
            out color);
    }

    public static void EvaluateVisual(
        ThreatAuraSegment aura,
        float3 ownerPosition,
        bool isEnraged,
        out float3 position,
        out float scale,
        out float4 color)
    {
        var speedMultiplier = ResolveAngularSpeedMultiplier(isEnraged);
        var angle = aura.Angle +
            aura.Elapsed * aura.AngularSpeed * speedMultiplier;
        math.sincos(angle, out var sin, out var cos);
        var pulse = 0.5f + 0.5f * math.sin(
            aura.Elapsed * (isEnraged ? 8f : 4.5f) + aura.Phase);
        position = ownerPosition + new float3(
            cos * aura.Radius,
            0.1f + pulse * 0.08f,
            sin * aura.Radius);
        scale = math.max(0.01f, aura.BaseScale) * math.lerp(
            isEnraged ? 0.9f : 0.82f,
            isEnraged ? 1.5f : 1.22f,
            pulse);
        color = isEnraged
            ? math.lerp(
                aura.BaseColor,
                new float4(1f, 0.06f, 0.32f, 1f),
                0.58f)
            : aura.BaseColor;
        color.w *= math.lerp(0.58f, 1f, pulse);
    }

    public static float ResolveAngularSpeedMultiplier(bool isEnraged)
    {
        return isEnraged ? 1.8f : 1f;
    }

    static void SetOrAddColor(
        EntityManager entityManager,
        Entity entity,
        float4 color)
    {
        var component = new URPMaterialPropertyBaseColor
        {
            Value = color,
        };
        if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
        {
            entityManager.SetComponentData(entity, component);
        }
        else
        {
            entityManager.AddComponentData(entity, component);
        }
    }

    static void RemoveIfPresent<T>(
        EntityManager entityManager,
        Entity entity)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
        {
            entityManager.RemoveComponent<T>(entity);
        }
    }
}

[BurstCompile]
[UpdateAfter(typeof(EnemyKnockbackSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ThreatAuraSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ThreatAuraSegment>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 ||
             runState.IsChoosingUpgrade != 0);
        var deltaTime = isPaused ? 0f : SystemAPI.Time.DeltaTime;
        var ownerTransforms =
            SystemAPI.GetComponentLookup<LocalTransform>(true);
        var championPhases =
            SystemAPI.GetComponentLookup<ChampionPhaseState>(true);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (aura, transform, color, entity) in
                 SystemAPI.Query<
                         RefRW<ThreatAuraSegment>,
                         RefRW<LocalTransform>,
                         RefRW<URPMaterialPropertyBaseColor>>()
                     .WithEntityAccess())
        {
            if (!ownerTransforms.HasComponent(aura.ValueRO.Owner))
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            aura.ValueRW.Elapsed += deltaTime;
            var isEnraged =
                championPhases.HasComponent(aura.ValueRO.Owner) &&
                championPhases[aura.ValueRO.Owner].IsEnraged != 0;
            ThreatAuraSpawnUtility.EvaluateVisual(
                aura.ValueRO,
                ownerTransforms[aura.ValueRO.Owner].Position,
                isEnraged,
                out var position,
                out var scale,
                out var nextColor);
            transform.ValueRW.Position = position;
            transform.ValueRW.Rotation = quaternion.RotateY(
                -aura.ValueRO.Angle -
                aura.ValueRO.Elapsed * aura.ValueRO.AngularSpeed *
                ThreatAuraSpawnUtility.ResolveAngularSpeedMultiplier(
                    isEnraged));
            transform.ValueRW.Scale = scale;
            color.ValueRW.Value = nextColor;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
