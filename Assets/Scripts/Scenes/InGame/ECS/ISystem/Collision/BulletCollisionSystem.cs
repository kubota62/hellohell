using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct BulletCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = true;
        
        state.RequireForUpdate<Bullet>();
        state.RequireForUpdate<Tank>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 弾とタンクの当たり判定
        foreach (var (bulletTransform, bullet, bulletEntity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<Bullet>>()
                     .WithAll<Bullet>()
                     .WithEntityAccess())
        {
            float3 bulletWorldPos = bulletTransform.ValueRO.Position;

            foreach (var (tankTransform, tankEntity) in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<Tank>()
                         .WithEntityAccess())
            {
                // 発射したタンクと同じタンクは判断除外
                if (tankEntity == bullet.ValueRO.Shooter) continue;
                
                float3 tankWorldPos = tankTransform.ValueRO.Position;
                if (math.distance(bulletWorldPos, tankWorldPos) < 2f)
                {
                    // 当たり
                    Debug.Log($"Bullet hit Tank {bulletEntity.Index} -> {tankEntity.Index}");
                    ECB.AppendToBuffer(tankEntity, new DamageEvent
                    {
                        Damage = 10,
                        Attacker = bulletEntity,
                    });
                }
            }
        }
    }
}