using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ProjectileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();  // Configがあるまで実行しない
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var projectileJob = new ProjectileMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = projectileJob.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct ProjectileMovementJob: IJobEntity
{
    public float DeltaTime;

    void Execute(ref ProjectileMotion motion, ref LocalTransform transform, ref Lifetime lifetime)
    {
        var gravity = new float3(0, -9.81f, 0);
        transform.Position += motion.Velocity * DeltaTime;
        
        if (transform.Position.y < 0)
        {
            lifetime.Remaining = 0f;
        }
        
        motion.Velocity += gravity * DeltaTime;
    }
}
