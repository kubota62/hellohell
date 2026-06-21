using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ActorDamageEventSystem : ISystem
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

        foreach (var (transform, health, damageEventBuffer) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<Health>, DynamicBuffer<DamageEvent>>()
                     .WithAll<ActorBody>())
        {
            var pos = transform.ValueRO.Position + new float3(0f, 1f, 0f);
            var totalDamage = 0;

            foreach (var damage in damageEventBuffer)
            {
                totalDamage += damage.Damage;

                DamageDigitSpawnUtility.SpawnDamageDigits(
                    ecb,
                    config.DamageDigitPrefab,
                    damage.Damage,
                    pos);
            }

            health.ValueRW.Current = math.max(0, health.ValueRO.Current - totalDamage);
            damageEventBuffer.Clear();
        }
    }
}
