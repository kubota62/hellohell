using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>敵が攻撃方向を固定してから発射するまでの溜め状態。</summary>
public struct EnemyAttackWindup : IComponentData
{
    public AttackMasterId AttackMasterId;
    public float3 Direction;
    public float Remaining;
    public float Duration;
}

/// <summary>攻撃方向を知らせる、判定を持たない発光マーカー。</summary>
public struct EnemyAttackTelegraphVisual : IComponentData
{
    public Entity Owner;
    public float3 Direction;
    public float Offset;
    public float BaseScale;
    public float4 BaseColor;
}

public static class EnemyAttackTelegraphUtility
{
    public static float ResolveDuration(
        bool isElite,
        bool isChampion,
        bool isFinalBoss,
        EliteAffixKind eliteAffix,
        bool isEnraged)
    {
        var duration = isFinalBoss
            ? 0.9f
            : isChampion
                ? 0.65f
                : isElite ? 0.45f : 0.28f;
        if (eliteAffix == EliteAffixKind.Swift)
        {
            duration *= 0.75f;
        }
        else if (eliteAffix == EliteAffixKind.Juggernaut)
        {
            duration *= 1.15f;
        }

        return math.max(0.12f, duration * (isEnraged ? 0.7f : 1f));
    }

    public static float4 ResolveColor(
        bool isElite,
        bool isChampion,
        bool isFinalBoss)
    {
        return isFinalBoss
            ? new float4(1f, 0.03f, 0.08f, 1f)
            : isChampion
                ? new float4(0.9f, 0.12f, 1f, 1f)
                : isElite
                    ? new float4(1f, 0.34f, 0.04f, 1f)
                    : new float4(1f, 0.78f, 0.08f, 1f);
    }

    public static void EvaluateVisual(
        in EnemyAttackTelegraphVisual visual,
        in EnemyAttackWindup windup,
        float3 ownerPosition,
        out float3 position,
        out float scale,
        out float4 color)
    {
        var duration = math.max(0.01f, windup.Duration);
        var progress = 1f - math.saturate(windup.Remaining / duration);
        var pulse = 0.5f + 0.5f * math.sin(
            progress * math.PI * 8f);
        position = ownerPosition +
            math.normalizesafe(
                visual.Direction,
                new float3(0f, 0f, 1f)) * visual.Offset;
        position.y += 0.28f + progress * 0.12f;
        scale = math.max(0.05f, visual.BaseScale) *
            math.lerp(0.42f, 1.3f, progress) *
            math.lerp(0.88f, 1.08f, pulse);
        color = math.lerp(
            visual.BaseColor,
            new float4(1f, 1f, 1f, 1f),
            math.saturate(progress * 0.72f + pulse * 0.12f));
        color.w = math.lerp(0.55f, 1f, progress);
    }

    public static void Begin(
        EntityManager entityManager,
        EntityCommandBuffer ecb,
        Entity visualPrefab,
        Entity owner,
        AttackMasterId attackMasterId,
        float3 direction)
    {
        var isFinalBoss = entityManager.HasComponent<FinalBossEnemy>(owner);
        var isChampion = entityManager.HasComponent<ChampionEnemy>(owner);
        var isElite = entityManager.HasComponent<EliteEnemy>(owner);
        var eliteAffix = entityManager.HasComponent<EliteAffix>(owner)
            ? entityManager.GetComponentData<EliteAffix>(owner).Kind
            : EliteAffixKind.None;
        var isEnraged = isFinalBoss
            ? entityManager.HasComponent<FinalBossPhaseState>(owner) &&
              entityManager.GetComponentData<FinalBossPhaseState>(owner)
                  .IsEnraged != 0
            : isChampion &&
              entityManager.HasComponent<ChampionPhaseState>(owner) &&
              entityManager.GetComponentData<ChampionPhaseState>(owner)
                  .IsEnraged != 0;
        var duration = ResolveDuration(
            isElite,
            isChampion,
            isFinalBoss,
            eliteAffix,
            isEnraged);
        var lockedDirection = direction;
        lockedDirection.y = 0f;
        lockedDirection = math.normalizesafe(
            lockedDirection,
            new float3(0f, 0f, 1f));
        var windup = new EnemyAttackWindup
        {
            AttackMasterId = attackMasterId,
            Direction = lockedDirection,
            Remaining = duration,
            Duration = duration,
        };
        ecb.AddComponent(owner, windup);

        if (visualPrefab == Entity.Null)
        {
            return;
        }

        var hitRadius = entityManager.HasComponent<Hitbox>(owner)
            ? entityManager.GetComponentData<Hitbox>(owner).Radius
            : 0.5f;
        var ownerPosition = entityManager.GetComponentData<LocalTransform>(
            owner).Position;
        var visual = new EnemyAttackTelegraphVisual
        {
            Owner = owner,
            Direction = lockedDirection,
            Offset = math.max(0.55f, hitRadius + 0.42f),
            BaseScale = math.max(0.18f, hitRadius * 0.62f),
            BaseColor = ResolveColor(
                isElite,
                isChampion,
                isFinalBoss),
        };
        EvaluateVisual(
            visual,
            windup,
            ownerPosition,
            out var position,
            out var scale,
            out var color);

        var visualEntity = ecb.Instantiate(visualPrefab);
        ecb.SetComponent(
            visualEntity,
            LocalTransform.FromPositionRotationScale(
                position,
                quaternion.LookRotationSafe(lockedDirection, math.up()),
                scale));
        var colorComponent = new URPMaterialPropertyBaseColor
        {
            Value = color,
        };
        if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(
                visualPrefab))
        {
            ecb.SetComponent(visualEntity, colorComponent);
        }
        else
        {
            ecb.AddComponent(visualEntity, colorComponent);
        }
        ecb.AddComponent(visualEntity, visual);
        RemoveGameplayComponents(ecb, visualEntity);
    }

    static void RemoveGameplayComponents(
        EntityCommandBuffer ecb,
        Entity entity)
    {
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

[BurstCompile]
[UpdateAfter(typeof(AttackRequestSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyAttackTelegraphVisualSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemyAttackTelegraphVisual>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ownerTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var windups = SystemAPI.GetComponentLookup<EnemyAttackWindup>(true);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (visual, transform, color, entity) in
                 SystemAPI.Query<
                         RefRO<EnemyAttackTelegraphVisual>,
                         RefRW<LocalTransform>,
                         RefRW<URPMaterialPropertyBaseColor>>()
                     .WithEntityAccess())
        {
            var owner = visual.ValueRO.Owner;
            if (!ownerTransforms.HasComponent(owner) ||
                !windups.HasComponent(owner))
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            var windup = windups[owner];
            EnemyAttackTelegraphUtility.EvaluateVisual(
                visual.ValueRO,
                windup,
                ownerTransforms[owner].Position,
                out var position,
                out var scale,
                out var nextColor);
            transform.ValueRW.Position = position;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(
                visual.ValueRO.Direction,
                math.up());
            transform.ValueRW.Scale = scale;
            color.ValueRW.Value = nextColor;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
