using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Expires temporary entities that use Lifetime.
/// Pooled entities can later replace the destroy path with GameplayActive disable/reuse.
/// </summary>
[BurstCompile]
public partial struct LifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Lifetime>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();

        var job = new LifetimeJob
        {
            ECB = ecb,
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct LifetimeJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;

    [BurstCompile]
    void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref Lifetime lifetime)
    {
        lifetime.Remaining -= DeltaTime;
        if (lifetime.Remaining <= 0f)
        {
            ECB.DestroyEntity(sortKey, entity);
        }
    }
}
