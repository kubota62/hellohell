using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Attack/Variants/MeleeArc の命中解決システム。
/// MeleeArcAttackRequest を消費し、扇形範囲内の敵へ DamageEvent を積む。
/// Actor 検索は空間ハッシュで近隣セルに絞り、敵数が増えても全件走査しない。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(AttackRequestSystem))]
public partial struct MeleeArcAttackSystem : ISystem
{
    private EntityQuery targetQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MeleeArcAttackRequest>();
        state.RequireForUpdate<SpatialHashSettings>();

        targetQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ActorBody, SpatialHashTarget, LocalTransform, Team, Hitbox>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<SpatialHashSettings>();
        var cellSize = math.max(0.001f, settings.CellSize);

        var targets = SpatialHashUtility.BuildTargetSnapshot(targetQuery, ref state, cellSize);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (request, requestEntity) in
                 SystemAPI.Query<RefRO<MeleeArcAttackRequest>>()
                     .WithEntityAccess())
        {
            ResolveMeleeArcRequest(
                ecb,
                request.ValueRO,
                cellSize,
                targets);

            CreateVfxRequest(ecb, request.ValueRO);
            ecb.DestroyEntity(requestEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static void ResolveMeleeArcRequest(
        EntityCommandBuffer ecb,
        MeleeArcAttackRequest request,
        float cellSize,
        SpatialHashSnapshot targets)
    {
        var requestCell = SpatialHashUtility.GetCell(request.Position, cellSize);
        var searchRadius = math.max(1, (int)math.ceil(request.Radius / cellSize) + 1);
        var forward = FlattenDirection(request.Direction);
        var halfAngleRadians = math.radians(math.clamp(request.AngleDegrees, 1f, 360f) * 0.5f);
        var minDot = math.cos(halfAngleRadians);

        for (var x = -searchRadius; x <= searchRadius; x++)
        {
            for (var z = -searchRadius; z <= searchRadius; z++)
            {
                var hash = SpatialHashUtility.GetHash(requestCell + new int2(x, z));
                if (!targets.Hash.TryGetFirstValue(hash, out var targetIndex, out var iterator))
                {
                    continue;
                }

                do
                {
                    TryDamageTarget(
                        ecb,
                        request,
                        forward,
                        minDot,
                        targets,
                        targetIndex);
                }
                while (targets.Hash.TryGetNextValue(out targetIndex, ref iterator));
            }
        }
    }

    private static void TryDamageTarget(
        EntityCommandBuffer ecb,
        MeleeArcAttackRequest request,
        float3 forward,
        float minDot,
        SpatialHashSnapshot targets,
        int targetIndex)
    {
        if (targets.Entities[targetIndex] == request.Owner) return;
        if (!TeamUtility.AreHostile(request.Team, targets.Teams[targetIndex].Value)) return;

        var toTarget = targets.Transforms[targetIndex].Position - request.Position;
        toTarget.y = 0f;
        var distanceSq = math.lengthsq(toTarget);
        var hitDistance = request.Radius + targets.Hitboxes[targetIndex].Radius;
        if (distanceSq > hitDistance * hitDistance)
        {
            return;
        }

        var targetDirection = math.normalizesafe(toTarget, forward);
        if (math.dot(forward, targetDirection) < minDot)
        {
            return;
        }

        ecb.AppendToBuffer(targets.Entities[targetIndex], new DamageEvent
        {
            Damage = request.Damage,
            Attacker = request.Owner,
        });
    }

    private static void CreateVfxRequest(EntityCommandBuffer ecb, MeleeArcAttackRequest request)
    {
        var entity = ecb.CreateEntity();
        ecb.AddComponent(entity, new MeleeArcVfxRequest
        {
            Position = request.Position,
            Direction = FlattenDirection(request.Direction),
            Radius = request.Radius,
            AngleDegrees = request.AngleDegrees,
            Duration = request.VisualDuration,
            Color = new float4(1f, 0.92f, 0.18f, 0.38f),
        });
    }

    private static float3 FlattenDirection(float3 direction)
    {
        direction.y = 0f;
        return math.normalizesafe(direction, new float3(0f, 0f, 1f));
    }
}
