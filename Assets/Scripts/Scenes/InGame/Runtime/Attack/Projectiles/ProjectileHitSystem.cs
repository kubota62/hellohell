using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Projectile と ActorBody の命中を空間ハッシュで判定し、命中先の DamageEvent バッファへダメージを積む。
/// 貫通弾は命中済み対象を記録し、残り貫通回数がある間は GameplayActive を維持する。
/// </summary>
[BurstCompile]
public partial struct ProjectileHitSystem : ISystem
{
    private EntityQuery targetQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = true;

        state.RequireForUpdate<ProjectileMotion>();
        state.RequireForUpdate<Projectile>();
        state.RequireForUpdate<ActorBody>();
        state.RequireForUpdate<SpatialHashSettings>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

        targetQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ActorBody, SpatialHashTarget, LocalTransform, Team, Hitbox>()
            .Build(ref state);
    }

    [BurstCompile]
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

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var collisionJob = new ProjectileHitJob
        {
            ECB = ecb,
            CellSize = cellSize,
            TargetHash = spatialHash,
            TargetEntities = targetEntities,
            TargetTransforms = targetTransforms,
            TargetTeams = targetTeams,
            TargetHitboxes = targetHitboxes
        };

        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(GameplayActive))]
public partial struct ProjectileHitJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float CellSize;

    [ReadOnly] public NativeParallelMultiHashMap<int, int> TargetHash;
    [ReadOnly] public NativeArray<Entity> TargetEntities;
    [ReadOnly] public NativeArray<LocalTransform> TargetTransforms;
    [ReadOnly] public NativeArray<Team> TargetTeams;
    [ReadOnly] public NativeArray<Hitbox> TargetHitboxes;

    [BurstCompile]
    private void Execute(
        [EntityIndexInQuery] int sortKey,
        Entity projectileEntity,
        in ProjectileMotion motion,
        in Projectile projectile,
        ref ProjectileModifierState modifierState,
        ref LocalTransform projectileTransform,
        DynamicBuffer<ProjectileHitRecord> hitRecords)
    {
        var projectilePos = projectileTransform.Position;
        var projectileCell = SpatialHashUtility.GetCell(projectilePos, CellSize);
        var searchRadius = math.max(1, (int)math.ceil(projectile.HitRadius / CellSize) + 1);

        for (var x = -searchRadius; x <= searchRadius; x++)
        {
            for (var z = -searchRadius; z <= searchRadius; z++)
            {
                var hash = SpatialHashUtility.GetHash(projectileCell + new int2(x, z));
                if (!TargetHash.TryGetFirstValue(hash, out var targetIndex, out var iterator))
                {
                    continue;
                }

                do
                {
                    if (TryHitTarget(
                            sortKey,
                            projectileEntity,
                            motion,
                            projectile,
                            ref modifierState,
                            ref projectileTransform,
                            hitRecords,
                            projectilePos,
                            targetIndex))
                    {
                        return;
                    }
                }
                while (TargetHash.TryGetNextValue(out targetIndex, ref iterator));
            }
        }
    }

    private bool TryHitTarget(
        int sortKey,
        Entity projectileEntity,
        in ProjectileMotion motion,
        in Projectile projectile,
        ref ProjectileModifierState modifierState,
        ref LocalTransform projectileTransform,
        DynamicBuffer<ProjectileHitRecord> hitRecords,
        float3 projectilePos,
        int targetIndex)
    {
        var targetEntity = TargetEntities[targetIndex];
        if (targetEntity == motion.Shooter) return false;
        if (WasAlreadyHit(hitRecords, targetEntity)) return false;
        if (!TeamUtility.AreHostile(projectile.Team, TargetTeams[targetIndex].Value)) return false;

        var targetPos = TargetTransforms[targetIndex].Position;
        var hitDistance = projectile.HitRadius + TargetHitboxes[targetIndex].Radius;

        if (math.distancesq(projectilePos, targetPos) >= hitDistance * hitDistance)
        {
            return false;
        }

        hitRecords.Add(new ProjectileHitRecord { Target = targetEntity });
        ECB.AppendToBuffer(sortKey, targetEntity, new DamageEvent
        {
            Damage = projectile.Damage,
            Attacker = projectile.Owner,
        });

        if (modifierState.PierceRemaining > 0)
        {
            modifierState.PierceRemaining--;
            return true;
        }

        projectileTransform.Position = new float3(0f, -1000f, 0f);
        projectileTransform.Scale = 0f;
        ECB.SetComponentEnabled<GameplayActive>(sortKey, projectileEntity, false);
        return true;
    }

    private static bool WasAlreadyHit(
        DynamicBuffer<ProjectileHitRecord> hitRecords,
        Entity target)
    {
        for (var i = 0; i < hitRecords.Length; i++)
        {
            if (hitRecords[i].Target == target)
            {
                return true;
            }
        }

        return false;
    }
}
