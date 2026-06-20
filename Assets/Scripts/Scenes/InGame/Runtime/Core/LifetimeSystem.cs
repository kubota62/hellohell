using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Lifetime を持つ一時エンティティを寿命切れにする。
/// プール対象は将来、破棄処理を GameplayActive の無効化と再利用に置き換えられる。
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
