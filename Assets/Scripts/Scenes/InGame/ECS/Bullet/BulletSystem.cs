using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct BulletSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();  // Configがあるまで実行しない
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var bulletJob = new BulletJob
        {
            ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged),
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = bulletJob.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct BulletJob: IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;

    void Execute(Entity entity, ref Bullet bullet, ref LocalTransform transform)
    {
        var gravity = new float3(0, -9.81f, 0);
        transform.Position += bullet.Velocity * DeltaTime;
        
        if (transform.Position.y < 0)
        {
            ECB.DestroyEntity(entity);
        }
        
        bullet.Velocity += gravity * DeltaTime;
    }
}
