using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

// この属性は、更新順序でこのシステムを TransformSystemGroup の前に置きます。
// ShootingSystem は砲弾のローカル変換のみを設定しますが、変換システムは
// TransformSystemGroup でワールド変換 (LocalToWorld) を設定します。
// フレーム内の TransformSystemGroup の後に ShootingSystem が更新された場合、砲弾は
// 生成されたオブジェクトは単一フレームの原点でレンダリングされます。
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ShootingSystem : ISystem
{
    private static readonly float ShootInterval = 1.0f;
    
    private float timer;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();  // Configがあるまで実行しない
        state.RequireForUpdate<PlayerInput>();  // 入力Entityが作られるまで実行しない
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

        foreach (var (tank, tankEntity) in
                 SystemAPI.Query<RefRO<Tank>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            Shoot(ref state, config, tankEntity, ecb);
        }
    }

    private void EnemyShoot(
        ref SystemState state,
        Config config,
        EntityCommandBuffer ecb)
    {
        foreach (var (tank, tankEntity) in
                 SystemAPI.Query<RefRO<Tank>>()
                     .WithAll<Enemy>()
                     .WithEntityAccess())
        {
            Shoot(ref state, config, tankEntity, ecb);
        }
    }

    private void Shoot(
        ref SystemState state,
        Config config,
        Entity tankEntity,
        EntityCommandBuffer ecb)
    {
        var tank = SystemAPI.GetComponent<Tank>(tankEntity);
        var canonLtw = SystemAPI.GetComponent<LocalToWorld>(tank.Canon);

        // 生成
        Entity bullet = ecb.Instantiate(config.BulletPrefab);

        // 位置
        var transform = LocalTransform.FromPosition(canonLtw.Position);
        transform.Scale = 0.5f;
        ecb.SetComponent(bullet, transform);

        // 色（あれば）
        if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(tankEntity))
        {
            var color = SystemAPI.GetComponent<URPMaterialPropertyBaseColor>(tankEntity);
            ecb.SetComponent(bullet, color);
        }

        // 弾データ
        var team = TeamId.Neutral;
        if (SystemAPI.HasComponent<Team>(tankEntity))
        {
            team = SystemAPI.GetComponent<Team>(tankEntity).Value;
        }

        ecb.SetComponent(bullet, new ProjectileMotion
        {
            Shooter = tankEntity,
            Velocity = math.normalize(canonLtw.Up) * 10f
        });
        ecb.SetComponent(bullet, new Projectile
        {
            Owner = tankEntity,
            Team = team,
            Damage = 34,
            HitRadius = 0.5f
        });
        ecb.SetComponent(bullet, new Lifetime
        {
            Remaining = 5f
        });
    }
}
