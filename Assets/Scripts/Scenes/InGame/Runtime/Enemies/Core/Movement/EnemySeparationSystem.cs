using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 大群内の近傍だけを空間ハッシュで調べ、敵が完全に重なるのを防ぐ。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(EnemyMovementSystem))]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EnemySeparationSystem : ISystem
{
    EntityQuery enemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        enemyQuery = SystemAPI.QueryBuilder()
            .WithAll<
                Enemy,
                ActorBody,
                LocalTransform,
                Team,
                Hitbox,
                GameplayActive>()
            .Build();
        state.RequireForUpdate<SpatialHashSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        if (enemyQuery.CalculateEntityCount() <= 1)
        {
            return;
        }

        var settings = SystemAPI.GetSingleton<SpatialHashSettings>();
        var cellSize = math.max(0.5f, settings.CellSize);
        var targets = SpatialHashUtility.BuildTargetSnapshot(
            enemyQuery,
            ref state,
            cellSize);
        var maximumHitRadius = 0f;
        for (var i = 0; i < targets.Hitboxes.Length; i++)
        {
            maximumHitRadius = math.max(
                maximumHitRadius,
                math.max(0.1f, targets.Hitboxes[i].Radius));
        }

        var job = new EnemySeparationJob
        {
            Targets = targets,
            CellSize = cellSize,
            MaximumHitRadius = maximumHitRadius,
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(
    typeof(Enemy),
    typeof(ActorBody),
    typeof(GameplayActive))]
[WithNone(typeof(Player))]
public partial struct EnemySeparationJob : IJobEntity
{
    const int MaximumNeighbors = 16;
    const float SeparationPadding = 1.04f;

    public SpatialHashSnapshot Targets;
    public float CellSize;
    public float MaximumHitRadius;

    [BurstCompile]
    public void Execute(
        Entity entity,
        in LocalTransform transform,
        in Hitbox hitbox,
        ref MoveIntent moveIntent)
    {
        var cell = SpatialHashUtility.GetCell(
            transform.Position,
            CellSize);
        var ownRadius = math.max(0.1f, hitbox.Radius);
        var searchRadius = math.clamp(
            (int)math.ceil(
                (ownRadius + MaximumHitRadius) /
                math.max(0.5f, CellSize)),
            1,
            3);
        var separation = float3.zero;
        var neighbors = 0;

        for (var x = -searchRadius;
             x <= searchRadius && neighbors < MaximumNeighbors;
             x++)
        {
            for (var z = -searchRadius;
                 z <= searchRadius && neighbors < MaximumNeighbors;
                 z++)
            {
                var hash = SpatialHashUtility.GetHash(
                    cell + new int2(x, z));
                if (!Targets.Hash.TryGetFirstValue(
                        hash,
                        out var targetIndex,
                        out var iterator))
                {
                    continue;
                }

                do
                {
                    var targetEntity = Targets.Entities[targetIndex];
                    if (targetEntity == entity)
                    {
                        continue;
                    }

                    var minimumDistance =
                        (ownRadius +
                         math.max(
                             0.1f,
                             Targets.Hitboxes[targetIndex].Radius)) *
                        SeparationPadding;
                    separation += ResolveContribution(
                        entity.Index,
                        targetEntity.Index,
                        transform.Position -
                            Targets.Transforms[targetIndex].Position,
                        minimumDistance);
                    neighbors++;
                }
                while (neighbors < MaximumNeighbors &&
                       Targets.Hash.TryGetNextValue(
                           out targetIndex,
                           ref iterator));
            }
        }

        moveIntent.Direction = BlendDirection(
            moveIntent.Direction,
            separation);
    }

    public static float3 ResolveContribution(
        int entityIndex,
        int neighborIndex,
        float3 offset,
        float minimumDistance)
    {
        offset.y = 0f;
        var safeDistance = math.max(0.01f, minimumDistance);
        var distanceSq = math.lengthsq(offset);
        if (distanceSq >= safeDistance * safeDistance)
        {
            return float3.zero;
        }

        if (distanceSq < 0.0001f)
        {
            return ResolveFallbackDirection(entityIndex, neighborIndex);
        }

        var distance = math.sqrt(distanceSq);
        return offset / distance *
            (1f - math.saturate(distance / safeDistance));
    }

    public static float3 BlendDirection(
        float3 movementDirection,
        float3 separation)
    {
        movementDirection.y = 0f;
        separation.y = 0f;
        if (math.lengthsq(separation) < 0.0001f)
        {
            return movementDirection;
        }

        var forward = math.normalizesafe(movementDirection);
        var pressure = math.saturate(math.length(separation));
        var separationDirection = math.normalizesafe(separation);
        if (math.lengthsq(forward) < 0.0001f)
        {
            return separationDirection;
        }

        return math.normalizesafe(
            forward +
            separationDirection * math.lerp(0.4f, 0.95f, pressure),
            forward);
    }

    public static float3 ResolveFallbackDirection(
        int entityIndex,
        int neighborIndex)
    {
        var lowerIndex = math.min(entityIndex, neighborIndex);
        var higherIndex = math.max(entityIndex, neighborIndex);
        var phase = math.frac(
            lowerIndex * 0.6180339f +
            higherIndex * 0.381966f) *
            math.PI * 2f;
        var direction = new float3(
            math.cos(phase),
            0f,
            math.sin(phase));
        return entityIndex <= neighborIndex
            ? direction
            : -direction;
    }
}
