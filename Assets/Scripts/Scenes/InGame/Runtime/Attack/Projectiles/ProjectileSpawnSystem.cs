using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// ProjectileAttackRequest を消費して Projectile エンティティを生成するシステム。
/// 攻撃の発射判断と、Projectile の具体的な初期化を分離する。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(AttackRequestSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<ProjectileAttackRequest>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (request, requestEntity) in
                 SystemAPI.Query<RefRO<ProjectileAttackRequest>>()
                     .WithEntityAccess())
        {
            var value = request.ValueRO;
            var projectileEntity = ecb.Instantiate(config.ProjectilePrefab);

            var transform = LocalTransform.FromPosition(value.Position);
            transform.Scale = value.Scale;
            ecb.SetComponent(projectileEntity, transform);

            if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(value.Owner))
            {
                var color = SystemAPI.GetComponent<URPMaterialPropertyBaseColor>(value.Owner);
                ecb.SetComponent(projectileEntity, color);
            }

            ecb.SetComponent(projectileEntity, new ProjectileMotion
            {
                Shooter = value.Owner,
                Velocity = math.normalizesafe(value.Direction) * value.Speed,
            });
            ecb.SetComponent(projectileEntity, new Projectile
            {
                Owner = value.Owner,
                Team = value.Team,
                Damage = value.Damage,
                HitRadius = value.HitRadius,
            });
            ecb.SetComponent(projectileEntity, new Lifetime
            {
                Remaining = value.Lifetime,
            });

            ecb.DestroyEntity(requestEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
