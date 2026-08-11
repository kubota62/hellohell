using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Attack/Variants/Projectiles の移動システム。
/// ProjectileMotion に従って Projectile をトップダウン平面上で移動する。
/// 寿命は LifetimeSystem が管理し、通常弾へ重力を加えないことで攻撃マスタの射程を保証する。
/// GameplayActive が有効な弾だけを処理し、非アクティブなプール待機弾は動かさない。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var projectileJob = new ProjectileMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = projectileJob.Schedule(state.Dependency);
    }
}

/// <summary>
/// アクティブなProjectileの位置、重力、寿命だけを更新する軽量Job。
/// </summary>
[BurstCompile]
[WithAll(typeof(GameplayActive))]
public partial struct ProjectileMovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(in ProjectileMotion motion, ref LocalTransform transform)
    {
        transform.Position = ProjectileMovementUtility.Advance(
            transform.Position,
            motion.Velocity,
            DeltaTime);
    }
}

public static class ProjectileMovementUtility
{
    public static float3 Advance(
        float3 position,
        float3 velocity,
        float deltaTime)
    {
        return position + velocity * math.max(0f, deltaTime);
    }
}
