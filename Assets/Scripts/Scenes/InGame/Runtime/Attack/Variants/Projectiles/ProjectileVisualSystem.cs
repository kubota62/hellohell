using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// 投射物の描画だけに使う状態。戦闘判定と分離し、見た目の伸縮が当たり判定へ影響しないようにする。
/// </summary>
public struct ProjectileVisualState : IComponentData
{
    public float4 BaseColor;
    public float BaseScale;
    public float Elapsed;
    public float Phase;
    public byte IsEnemy;
}

/// <summary>
/// 敵弾を危険色、進行方向へ伸びた形、脈動で識別しやすくする純粋計算群。
/// </summary>
public static class ProjectileVisualUtility
{
    public static readonly float4 EnemyDangerColor =
        new(1f, 0.08f, 0.2f, 1f);

    const float EnemyPulseSpeed = 11f;
    const float EnemyPulseAmplitude = 0.18f;

    public static ProjectileVisualState Create(
        TeamId team,
        float baseScale,
        float3 position,
        float4 baseColor)
    {
        return new ProjectileVisualState
        {
            BaseColor = baseColor,
            BaseScale = math.max(0.01f, baseScale),
            Phase = ResolvePhase(position),
            IsEnemy = team == TeamId.Enemy ? (byte)1 : (byte)0,
        };
    }

    public static float4 ResolveBaseColor(TeamId team, float4 ownerColor)
    {
        var safeOwnerColor = math.saturate(ownerColor);
        safeOwnerColor.w = 1f;
        if (team != TeamId.Enemy)
        {
            return safeOwnerColor;
        }

        var dangerColor = math.lerp(
            safeOwnerColor,
            EnemyDangerColor,
            0.82f);
        dangerColor.w = 1f;
        return dangerColor;
    }

    public static float EvaluateScale(ProjectileVisualState visual)
    {
        if (visual.IsEnemy == 0)
        {
            return math.max(0.01f, visual.BaseScale);
        }

        var pulse = 0.5f + 0.5f * math.sin(
            math.max(0f, visual.Elapsed) * EnemyPulseSpeed +
            visual.Phase);
        return math.max(0.01f, visual.BaseScale) *
            (1f + pulse * EnemyPulseAmplitude);
    }

    public static float4 EvaluateColor(ProjectileVisualState visual)
    {
        if (visual.IsEnemy == 0)
        {
            return visual.BaseColor;
        }

        var pulse = 0.5f + 0.5f * math.sin(
            math.max(0f, visual.Elapsed) * EnemyPulseSpeed +
            visual.Phase);
        var color = math.lerp(
            visual.BaseColor,
            new float4(1f, 0.62f, 0.68f, 1f),
            pulse * 0.24f);
        color.w = 1f;
        return color;
    }

    public static float4x4 ResolveShape(TeamId team)
    {
        return team == TeamId.Enemy
            ? float4x4.Scale(new float3(0.72f, 0.72f, 1.9f))
            : float4x4.identity;
    }

    public static quaternion ResolveRotation(float3 direction)
    {
        var forward = math.normalizesafe(direction, new float3(0f, 0f, 1f));
        var up = math.abs(math.dot(forward, math.up())) > 0.98f
            ? new float3(0f, 0f, 1f)
            : math.up();
        return quaternion.LookRotationSafe(forward, up);
    }

    static float ResolvePhase(float3 position)
    {
        var seed = math.abs(
            position.x * 0.1031f +
            position.y * 0.0973f +
            position.z * 0.11369f);
        return math.frac(seed) * math.PI * 2f;
    }
}

/// <summary>
/// アクティブな敵弾を速度方向へ向け、色と大きさを脈動させる。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ProjectileMovementSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileVisualSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ProjectileVisualState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        state.Dependency = new ProjectileVisualJob
        {
            DeltaTime = math.max(0f, SystemAPI.Time.DeltaTime),
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(GameplayActive))]
public partial struct ProjectileVisualJob : IJobEntity
{
    public float DeltaTime;

    void Execute(
        ref ProjectileVisualState visual,
        ref LocalTransform transform,
        ref URPMaterialPropertyBaseColor color,
        in ProjectileMotion motion)
    {
        visual.Elapsed += DeltaTime;
        transform.Scale = ProjectileVisualUtility.EvaluateScale(visual);
        color.Value = ProjectileVisualUtility.EvaluateColor(visual);

        if (visual.IsEnemy != 0)
        {
            transform.Rotation = ProjectileVisualUtility.ResolveRotation(
                motion.Velocity);
        }
    }
}
