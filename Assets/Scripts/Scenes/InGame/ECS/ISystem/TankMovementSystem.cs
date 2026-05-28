using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct TankMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();  // Configがあるまで実行しない
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UpdateTankMovement(ref state);
        UpdateTurretRotation(ref state);
    }

    [BurstCompile]
    private void UpdateTankMovement(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<Tank>()
                     .WithNone<Player>()  // プレイヤー除外
                     .WithEntityAccess())
        {
            var pos = transform.ValueRO.Position;
            pos.y += (float)entity.Index;

            var angle = (0.5f + noise.cnoise(pos * 0.2f)) * math.PI * 2;
            var dir = float3.zero;
            math.sincos(angle, out dir.x, out dir.z);

            // UnityEngine.Debug.Log($"pos: {transform.ValueRO.Position}, angle: {angle}, dir: {dir}");
            transform.ValueRW.Position += dir * dt * 2.0f;
            transform.ValueRW.Rotation = quaternion.RotateY(angle);
        }
    }

    [BurstCompile]
    public void UpdateTurretRotation(ref SystemState state)
    {
        var spin = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI);
        foreach (var tank in
                 SystemAPI.Query<RefRW<Tank>>())
        {
            var trans = SystemAPI.GetComponentRW<LocalTransform>(tank.ValueRO.Turret);
    
            // Add a rotation around the Y axis (relative to the parent).
            trans.ValueRW.Rotation = math.mul(spin, trans.ValueRO.Rotation);
        }
    }
}