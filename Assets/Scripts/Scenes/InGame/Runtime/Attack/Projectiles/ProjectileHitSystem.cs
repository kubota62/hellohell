using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Projectile と ActorBody の命中を判定し、命中先の DamageEvent バッファへダメージを積む。
/// Team で敵対関係を判定するため、Player/Enemy の種類を直接見ずに再利用できる。
/// </summary>
[BurstCompile]
public partial struct ProjectileHitSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = true;

        state.RequireForUpdate<ProjectileMotion>();
        state.RequireForUpdate<Projectile>();
        state.RequireForUpdate<ActorBody>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var targetQuery = SystemAPI.QueryBuilder().WithAll<ActorBody, LocalTransform, Team, Hitbox>().Build();

        var targetEntities = targetQuery.ToEntityArray(state.WorldUpdateAllocator);
        var targetTransforms = targetQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
        var targetTeams = targetQuery.ToComponentDataArray<Team>(state.WorldUpdateAllocator);
        var targetHitboxes = targetQuery.ToComponentDataArray<Hitbox>(state.WorldUpdateAllocator);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var collisionJob = new ProjectileHitJob
        {
            ECB = ecb,
            TargetEntities = targetEntities,
            TargetTransforms = targetTransforms,
            TargetTeams = targetTeams,
            TargetHitboxes = targetHitboxes
        };

        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct ProjectileHitJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

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
        in LocalTransform projectileTransform)
    {
        var projectilePos = projectileTransform.Position;

        for (int i = 0; i < TargetEntities.Length; i++)
        {
            if (TargetEntities[i] == motion.Shooter) continue;
            if (!TeamUtility.AreHostile(projectile.Team, TargetTeams[i].Value)) continue;

            var targetPos = TargetTransforms[i].Position;
            var hitDistance = projectile.HitRadius + TargetHitboxes[i].Radius;

            if (math.distancesq(projectilePos, targetPos) < hitDistance * hitDistance)
            {
                ECB.AppendToBuffer(sortKey, TargetEntities[i], new DamageEvent
                {
                    Damage = projectile.Damage,
                    Attacker = projectile.Owner,
                });

                ECB.DestroyEntity(sortKey, projectileEntity);
            }
        }
    }
}
