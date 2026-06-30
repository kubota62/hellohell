using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// ProjectileMotion に従って Projectile を移動し、地面より下へ落ちた弾の寿命を切る。
/// GameplayActive が有効な弾だけを処理し、非アクティブなプール待機弾は動かさない。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
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
[WithAll(typeof(GameplayActive))]
public partial struct ProjectileMovementJob : IJobEntity
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
