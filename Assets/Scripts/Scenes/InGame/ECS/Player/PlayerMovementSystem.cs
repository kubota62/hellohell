using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Reads the shared PlayerInput entity and moves the Player tank.
/// Runs before enemy movement so AI reads the latest Player position.
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TankMovementForwardSystem))]
[UpdateBefore(typeof(TankMovementRandomSystem))]
public partial struct PlayerMovementSystem : ISystem
{
    const float MoveSpeed = 5f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();
        if (math.lengthsq(input.Movement) < 0.0001f)
        {
            return;
        }

        var direction = math.normalizesafe(new float3(input.Movement.x, 0f, input.Movement.y));
        var job = new PlayerMovementJob
        {
            Direction = direction,
            DeltaTime = SystemAPI.Time.DeltaTime,
            Speed = MoveSpeed
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// Moves only tanks tagged as Player.
/// The system converts input into a world-space XZ direction before scheduling this job.
/// </summary>
[BurstCompile]
[WithAll(typeof(Player))]
public partial struct PlayerMovementJob : IJobEntity
{
    public float3 Direction;
    public float DeltaTime;
    public float Speed;

    [BurstCompile]
    public void Execute(ref LocalTransform transform)
    {
        transform.Position += Direction * Speed * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(Direction, math.up());
    }
}
