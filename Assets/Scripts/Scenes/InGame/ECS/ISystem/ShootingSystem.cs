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
    private static readonly float ShootTimer = 0.6f;
    
    private float timer;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0)
        {
            return;
        }

        timer = ShootTimer;

        var config = SystemAPI.GetSingleton<Config>();
        var ballTransform = state.EntityManager.GetComponentData<LocalTransform>(config.BulletPrefab);

        PlayerShoot(ref state, config, ballTransform);
        EnemyShoot(ref state, config, ballTransform);
    }

    [BurstCompile]
    private void EnemyShoot(ref SystemState state, Config config, LocalTransform ballTransform)
    {
        // すべての戦車の砲塔ごとに砲弾を生成し、その初速度を設定します
        foreach (var (tank, color, tankEntity) in
                 SystemAPI.Query<RefRO<Tank>, RefRO<URPMaterialPropertyBaseColor>>()
                     .WithAll<Tank>()
                     .WithNone<Player>()
                     .WithEntityAccess())
        {
            Shoot(state, config.BulletPrefab, tankEntity, color.ValueRO, tank.ValueRO, ballTransform);
        }
    }

    [BurstCompile]
    private void PlayerShoot(ref SystemState state, Config config, LocalTransform ballTransform)
    {
        // すべての戦車の砲塔ごとに砲弾を生成し、その初速度を設定します
        foreach (var (tank, color, tankEntity) in
                 SystemAPI.Query<RefRO<Tank>, RefRO<URPMaterialPropertyBaseColor>>()
                    .WithAll<Player>()
                    .WithEntityAccess())
        {
            var input = SystemAPI.GetSingleton<PlayerInput>();
            if (input.IsFire)
            {
                Shoot(state, config.BulletPrefab, tankEntity, color.ValueRO, tank.ValueRO, ballTransform);
            }
        }
    }

    [BurstCompile]
    private void Shoot(
        SystemState state,
        Entity bulletPrefab,
        Entity tankEntity,
        URPMaterialPropertyBaseColor color,
        Tank tank,
        LocalTransform ballTransform)
    {
        Entity bulletEntity = state.EntityManager.Instantiate(bulletPrefab);

        // 砲弾を発射した戦車に合わせて砲弾の色を設定します。
        state.EntityManager.SetComponentData(bulletEntity, color);

        // ワールド空間での大砲の変換が必要なので、LocalTransform の代わりに LocalToWorld を取得します。
        var canonTransform = state.EntityManager.GetComponentData<LocalToWorld>(tank.Canon);
        ballTransform.Position = canonTransform.Position;

        state.EntityManager.SetComponentData(bulletEntity, ballTransform);
        state.EntityManager.SetComponentData(bulletEntity,
            new Bullet
            {
                Shooter = tankEntity,
                Velocity = math.normalize(canonTransform.Up) * 10f
            }
        );
    }
}