using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// Player と Enemy の ActorBody から Projectile 攻撃を発射するシステム。
/// 将来は WeaponDefinition や AbilityDefinition から発射条件と攻撃内容を受け取る想定。
/// </summary>
// この属性は、更新順序でこのシステムを TransformSystemGroup の前に置く。
// 発射位置に LocalToWorld を使うため、Projectile の生成は変換更新より前に済ませる。
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ShootingSystem : ISystem
{
    private static readonly float ShootInterval = 1.0f;

    private float timer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0) return;
        timer = ShootInterval;

        var config = SystemAPI.GetSingleton<Config>();
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        PlayerShoot(ref state, config, ecb);
        EnemyShoot(ref state, config, ecb);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void PlayerShoot(
        ref SystemState state,
        Config config,
        EntityCommandBuffer ecb)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();
        if (!input.IsFire) return;

        foreach (var (_, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            Shoot(ref state, config, actorEntity, ecb);
        }
    }

    private void EnemyShoot(
        ref SystemState state,
        Config config,
        EntityCommandBuffer ecb)
    {
        foreach (var (_, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>>()
                     .WithAll<Enemy>()
                     .WithEntityAccess())
        {
            Shoot(ref state, config, actorEntity, ecb);
        }
    }

    private void Shoot(
        ref SystemState state,
        Config config,
        Entity actorEntity,
        EntityCommandBuffer ecb)
    {
        var actorBody = SystemAPI.GetComponent<ActorBody>(actorEntity);
        var canonLtw = SystemAPI.GetComponent<LocalToWorld>(actorBody.Canon);

        var projectileEntity = ecb.Instantiate(config.ProjectilePrefab);

        var transform = LocalTransform.FromPosition(canonLtw.Position);
        transform.Scale = 0.5f;
        ecb.SetComponent(projectileEntity, transform);

        if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(actorEntity))
        {
            var color = SystemAPI.GetComponent<URPMaterialPropertyBaseColor>(actorEntity);
            ecb.SetComponent(projectileEntity, color);
        }

        var team = TeamId.Neutral;
        if (SystemAPI.HasComponent<Team>(actorEntity))
        {
            team = SystemAPI.GetComponent<Team>(actorEntity).Value;
        }

        ecb.SetComponent(projectileEntity, new ProjectileMotion
        {
            Shooter = actorEntity,
            Velocity = math.normalize(canonLtw.Up) * 10f
        });
        ecb.SetComponent(projectileEntity, new Projectile
        {
            Owner = actorEntity,
            Team = team,
            Damage = 34,
            HitRadius = 0.5f
        });
        ecb.SetComponent(projectileEntity, new Lifetime
        {
            Remaining = 5f
        });
    }
}
