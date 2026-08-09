using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 経験値・回復ドロップの浮遊、脈動、回転に必要な表示状態。
/// </summary>
public struct RewardPickupVisual : IComponentData
{
    public float Elapsed;
    public float Phase;
    public float BaseHeight;
    public float BaseScale;
    public float BobAmplitude;
    public float PulseAmplitude;
    public float SpinSpeed;
}

/// <summary>
/// 地面に残る報酬を静止させず、遠目でも判別しやすいオーブとして動かす。
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(HealthPickupSystem))]
[UpdateBefore(typeof(ExperiencePickupSystem))]
public partial struct RewardPickupVisualSystem : ISystem
{
    const float BobFrequency = 3.5f;
    const float PulseFrequency = 5f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RewardPickupVisual>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (visual, transform) in
                 SystemAPI.Query<
                     RefRW<RewardPickupVisual>,
                     RefRW<LocalTransform>>())
        {
            visual.ValueRW.Elapsed += deltaTime;
            var elapsed = visual.ValueRO.Elapsed;
            var position = transform.ValueRO.Position;
            position.y = EvaluateHeight(visual.ValueRO, elapsed);

            transform.ValueRW.Position = position;
            transform.ValueRW.Scale = EvaluateScale(
                visual.ValueRO,
                elapsed);
            transform.ValueRW.Rotation = math.mul(
                quaternion.RotateY(deltaTime * visual.ValueRO.SpinSpeed),
                transform.ValueRO.Rotation);
        }
    }

    public static RewardPickupVisual Create(
        float3 position,
        float baseScale,
        bool isChampion,
        bool isHealth)
    {
        return new RewardPickupVisual
        {
            Phase = ResolvePhase(position),
            BaseHeight = position.y,
            BaseScale = math.max(0.01f, baseScale),
            BobAmplitude = isChampion ? 0.22f : isHealth ? 0.16f : 0.12f,
            PulseAmplitude = isChampion ? 0.18f : isHealth ? 0.14f : 0.1f,
            SpinSpeed = isChampion ? 4.5f : isHealth ? 3.5f : 2.5f,
        };
    }

    public static float EvaluateHeight(
        RewardPickupVisual visual,
        float elapsed)
    {
        return visual.BaseHeight +
            math.sin(
                math.max(0f, elapsed) * BobFrequency +
                visual.Phase) *
            math.max(0f, visual.BobAmplitude);
    }

    public static float EvaluateScale(
        RewardPickupVisual visual,
        float elapsed)
    {
        var pulse = 0.5f + 0.5f * math.sin(
            math.max(0f, elapsed) * PulseFrequency +
            visual.Phase);
        return math.max(0.01f, visual.BaseScale) *
            (1f + pulse * math.max(0f, visual.PulseAmplitude));
    }

    static float ResolvePhase(float3 position)
    {
        var seed = math.abs(
            position.x * 0.1031f +
            position.z * 0.11369f);
        return math.frac(seed) * math.PI * 2f;
    }
}
