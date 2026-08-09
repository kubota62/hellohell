using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Enemies/Core/Movement の共通移動適用システム。
/// 各Enemy移動バリエーションが書き込んだ MoveIntent を、実際の座標と向きへ反映する。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveIntentApplySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var job = new EnemyMoveIntentApplyJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// Enemy共通の移動意思を消費するJob。
/// AI別の判断は Variants 側で済ませ、ここでは座標更新とMoveIntentのクリアだけを行う。
/// </summary>
[BurstCompile]
[WithAll(typeof(ActorBody))]
[WithAll(typeof(Enemy))]
[WithNone(typeof(Player))]
public partial struct EnemyMoveIntentApplyJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, ref MoveIntent moveIntent)
    {
        if (moveIntent.Magnitude <= 0f || math.lengthsq(moveIntent.Direction) < 0.0001f)
        {
            moveIntent.Direction = float3.zero;
            moveIntent.Magnitude = 0f;
            return;
        }

        var direction = math.normalizesafe(moveIntent.Direction);
        transform.Position += direction * moveIntent.Magnitude * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(direction, math.up());

        moveIntent.Direction = float3.zero;
        moveIntent.Magnitude = 0f;
    }
}

/// <summary>攻撃を受けた敵へ加算される、XZ平面上の押し戻し速度。</summary>
public struct EnemyKnockback : IComponentData
{
    public float3 Velocity;
}

/// <summary>
/// 通常移動の後にノックバックを適用し、短時間で滑らかに減衰させる。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(EnemyMoveIntentApplySystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyKnockbackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var job = new EnemyKnockbackJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(ActorBody), typeof(Enemy))]
[WithNone(typeof(Player))]
public partial struct EnemyKnockbackJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(
        ref LocalTransform transform,
        ref EnemyKnockback knockback)
    {
        Advance(ref transform, ref knockback, DeltaTime);
    }

    public static void Advance(
        ref LocalTransform transform,
        ref EnemyKnockback knockback,
        float deltaTime)
    {
        var safeDeltaTime = math.max(0f, deltaTime);
        if (math.lengthsq(knockback.Velocity) < 0.0025f)
        {
            knockback.Velocity = float3.zero;
            return;
        }

        transform.Position += knockback.Velocity * safeDeltaTime;
        knockback.Velocity *= math.exp(-12f * safeDeltaTime);
    }
}
