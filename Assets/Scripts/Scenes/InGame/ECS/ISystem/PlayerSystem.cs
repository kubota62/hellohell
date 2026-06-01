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
        // PlayerInputをもったentityがある場合だけ更新
        state.RequireForUpdate<PlayerInput>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // PlayerInputから入力情報を取得
        var input = SystemAPI.GetSingleton<PlayerInput>();
        
        var movement = new float3(
            input.Movement.x,
            0,
            input.Movement.y
        );
        movement *= SystemAPI.Time.DeltaTime * 5f;

        // Playerコンポーネントの付いているすべてのentityからLocalTransformを取得
        foreach (var playerTransform in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<Player>())
        {
            playerTransform.ValueRW.Position += movement;
        }
    }
}