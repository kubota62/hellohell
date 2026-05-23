using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct BulletSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var bulletJob = new BulletJob
        {
            ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged),
            DateTime = SystemAPI.Time.DeltaTime
        };
        
        bulletJob.Schedule();
    }
}

[BurstCompile]
public partial struct BulletJob: IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DateTime;

    void Execute(Entity entity, ref Bullet bullet, ref LocalTransform transform)
    {
        var gravity = new float3(0, -9.81f, 0);
        transform.Position += bullet.Velocity * DateTime;
        
        if (transform.Position.y < 0)
        {
            ECB.DestroyEntity(entity);
        }
        
        bullet.Velocity += gravity * DateTime;
    }


}