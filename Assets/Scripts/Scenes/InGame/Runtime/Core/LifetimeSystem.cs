using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Lifetime を持つ一時エンティティを寿命切れにする。
/// PooledInstance は Destroy せず GameplayActive を無効化し、それ以外は従来通り破棄する。
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

        var deltaTime = SystemAPI.Time.DeltaTime;
        state.Dependency = new PooledLifetimeJob
        {
            ECB = ecb,
            DeltaTime = deltaTime
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new DestroyLifetimeJob
        {
            ECB = ecb,
            DeltaTime = deltaTime
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(PooledInstance))]
[WithAll(typeof(GameplayActive))]
public partial struct PooledLifetimeJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;

    [BurstCompile]
    void Execute(
        [EntityIndexInQuery] int sortKey,
        Entity entity,
        ref Lifetime lifetime,
        ref LocalTransform transform)
    {
        lifetime.Remaining -= DeltaTime;
        if (lifetime.Remaining <= 0f)
        {
            transform.Position = new float3(0f, -1000f, 0f);
            transform.Scale = 0f;
            ECB.SetComponentEnabled<GameplayActive>(sortKey, entity, false);
        }
    }
}

[BurstCompile]
[WithNone(typeof(PooledInstance))]
public partial struct DestroyLifetimeJob : IJobEntity
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
