using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct TankDamageEventSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, damageEventBuffer) in
                 SystemAPI.Query<RefRO<LocalTransform>, DynamicBuffer<DamageEvent>>()
                     .WithAll<Tank>())
        {
            var pos = transform.ValueRO.Position + new float3(0f, 1f, 0f);

            foreach (var damage in damageEventBuffer)
            {
                DamageDigitSpawnUtility.SpawnDamageDigits(
                    ecb,
                    config.DamageDigitPrefab,
                    damage.Damage,
                    pos);
            }

            damageEventBuffer.Clear();
        }
    }
}
