using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PlayerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInput>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();
        
        var movement = new float3(
            input.Movement.x,
            0,
            input.Movement.y
        );
        movement *= SystemAPI.Time.DeltaTime * 5f;

        foreach (var playerTransform in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<Player>())
        {
            playerTransform.ValueRW.Position += movement;
        }
    }
}