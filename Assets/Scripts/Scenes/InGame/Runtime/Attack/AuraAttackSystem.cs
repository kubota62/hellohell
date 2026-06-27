using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// AuraAttackRequest を消費し、範囲内の敵 Actor へ DamageEvent を積むシステム。
/// Actor の検索は空間ハッシュで近傍セルに絞り、敵数が増えても総当たりにならないようにする。
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

        var targetEntities = targetQuery.ToEntityArray(state.WorldUpdateAllocator);
        var targetTransforms = targetQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
        var targetTeams = targetQuery.ToComponentDataArray<Team>(state.WorldUpdateAllocator);
        var targetHitboxes = targetQuery.ToComponentDataArray<Hitbox>(state.WorldUpdateAllocator);

        var spatialHash = new NativeParallelMultiHashMap<int, int>(
            math.max(1, targetEntities.Length),
            state.WorldUpdateAllocator);

        for (var i = 0; i < targetEntities.Length; i++)
        {
            var hash = SpatialHashUtility.GetHash(targetTransforms[i].Position, cellSize);
            spatialHash.Add(hash, i);
        }

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
                spatialHash,
                targetEntities,
                targetTransforms,
                targetTeams,
                targetHitboxes);

            ecb.DestroyEntity(requestEntity);
        }
    }

    private static void ResolveAuraRequest(
        EntityCommandBuffer ecb,
        AuraAttackRequest request,
        float cellSize,
        NativeParallelMultiHashMap<int, int> spatialHash,
        NativeArray<Entity> targetEntities,
        NativeArray<LocalTransform> targetTransforms,
        NativeArray<Team> targetTeams,
        NativeArray<Hitbox> targetHitboxes)
    {
        var requestCell = SpatialHashUtility.GetCell(request.Position, cellSize);
        var searchRadius = math.max(1, (int)math.ceil(request.Radius / cellSize) + 1);

        for (var x = -searchRadius; x <= searchRadius; x++)
        {
            for (var z = -searchRadius; z <= searchRadius; z++)
            {
                var hash = SpatialHashUtility.GetHash(requestCell + new int2(x, z));
                if (!spatialHash.TryGetFirstValue(hash, out var targetIndex, out var iterator))
                {
                    continue;
                }

                do
                {
                    TryDamageTarget(
                        ecb,
                        request,
                        targetEntities,
                        targetTransforms,
                        targetTeams,
                        targetHitboxes,
                        targetIndex);
                }
                while (spatialHash.TryGetNextValue(out targetIndex, ref iterator));
            }
        }
    }

    private static void TryDamageTarget(
        EntityCommandBuffer ecb,
        AuraAttackRequest request,
        NativeArray<Entity> targetEntities,
        NativeArray<LocalTransform> targetTransforms,
        NativeArray<Team> targetTeams,
        NativeArray<Hitbox> targetHitboxes,
        int targetIndex)
    {
        if (targetEntities[targetIndex] == request.Owner) return;
        if (!TeamUtility.AreHostile(request.Team, targetTeams[targetIndex].Value)) return;

        var hitRadius = request.Radius + targetHitboxes[targetIndex].Radius;
        if (math.distancesq(request.Position, targetTransforms[targetIndex].Position) > hitRadius * hitRadius)
        {
            return;
        }

        ecb.AppendToBuffer(targetEntities[targetIndex], new DamageEvent
        {
            Damage = request.Damage,
            Attacker = request.Owner,
        });
    }
}
