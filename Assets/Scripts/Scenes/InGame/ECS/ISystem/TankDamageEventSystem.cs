using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct TankDamageEventSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UpdateTankMovement(ref state);
    }

    [BurstCompile]
    private void UpdateTankMovement(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, damageEventBuffer, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<DamageEvent>>()
                     .WithAll<Tank, DamageEvent>()
                     .WithEntityAccess())
        {
            var pos = transform.ValueRO.Position;
            foreach (var damage in damageEventBuffer)
            {
            }
            
            damageEventBuffer.Clear();
        }
    }
}