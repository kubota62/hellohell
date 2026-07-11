using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Attack/Variants/Aura の命中解決システム。
/// AuraAttackRequest を消費し、範囲内の敵 Actor へ DamageEvent を積む。
/// Actor の検索は空間ハッシュで近隣セルに絞り、数が増えても総当たりにならないようにする。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(AttackRequestSystem))]
public partial struct AuraAttackSystem : ISystem
{
    private EntityQuery targetQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AuraAttackRequest>();
        state.RequireForUpdate<SpatialHashSettings>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

        targetQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ActorBody, SpatialHashTarget, LocalTransform, Team, Hitbox>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<SpatialHashSettings>();
        var cellSize = math.max(0.001f, settings.CellSize);

        var targets = SpatialHashUtility.BuildTargetSnapshot(targetQuery, ref state, cellSize);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (request, requestEntity) in
                 SystemAPI.Query<RefRO<AuraAttackRequest>>()
                     .WithEntityAccess())
        {
            ResolveAuraRequest(
                ecb,
                request.ValueRO,
                cellSize,
                targets);

            ecb.DestroyEntity(requestEntity);
        }
    }

    private static void ResolveAuraRequest(
        EntityCommandBuffer ecb,
        AuraAttackRequest request,
        float cellSize,
        SpatialHashSnapshot targets)
    {
        var requestCell = SpatialHashUtility.GetCell(request.Position, cellSize);
        var searchRadius = math.max(1, (int)math.ceil(request.Radius / cellSize) + 1);

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
                        targets,
                        targetIndex);
                }
                while (targets.Hash.TryGetNextValue(out targetIndex, ref iterator));
            }
        }
    }

    private static void TryDamageTarget(
        EntityCommandBuffer ecb,
        AuraAttackRequest request,
        SpatialHashSnapshot targets,
        int targetIndex)
    {
        if (targets.Entities[targetIndex] == request.Owner) return;
        if (!TeamUtility.AreHostile(request.Team, targets.Teams[targetIndex].Value)) return;

        var hitRadius = request.Radius + targets.Hitboxes[targetIndex].Radius;
        if (math.distancesq(request.Position, targets.Transforms[targetIndex].Position) > hitRadius * hitRadius)
        {
            return;
        }

        ecb.AppendToBuffer(targets.Entities[targetIndex], new DamageEvent
        {
            Damage = request.Damage,
            Attacker = request.Owner,
        });
    }
}
