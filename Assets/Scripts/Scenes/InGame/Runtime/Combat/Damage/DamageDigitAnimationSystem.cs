using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// ダメージ数値表示を浮かせてフェードアウトさせる演出システム。
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DamageDigitAnimationSystem : ISystem
{
    // 俯瞰カメラの表示面に合わせ、数字用Quadが画面正面を向く固定回転。
    // Unity Transform の Euler と mathematics の Euler は回転順でズレるため、シーン上のカメラ姿勢を Quaternion で固定する。
    static readonly quaternion BillboardRotation =
        new quaternion(0.40821788f, -0.23456968f, 0.10938163f, 0.8754261f);

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DamageDigit>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (digit, transform, color, entity) in
                 SystemAPI.Query<RefRW<DamageDigit>, RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>>()
                     .WithEntityAccess())
        {
            digit.ValueRW.Elapsed += dt;
            var elapsed = digit.ValueRO.Elapsed;

            if (elapsed >= digit.ValueRO.Lifetime)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            var startPos = digit.ValueRO.StartPosition;
            var offset = digit.ValueRO.HorizontalOffset;
            var localOffset = math.rotate(BillboardRotation, new float3(offset, 0f, 0f));

            float y = startPos.y;
            float uniformScale = 1f;
            float alpha = 1f;

            if (elapsed < 0.25f)
            {
                var t = elapsed / 0.25f;
                y = startPos.y + OutQuad(t) * 2f;
                uniformScale = math.lerp(0.5f, 1.2f, math.saturate(elapsed / 0.1f));
            }
            else if (elapsed < 0.3f)
            {
                y = startPos.y + 2f;
                uniformScale = math.lerp(1.2f, 1f, (elapsed - 0.25f) / 0.05f);
            }
            else
            {
                y = startPos.y + 2f;
                var t = (elapsed - 0.3f) / 0.1f;
                alpha = 1f - InQuad(math.saturate(t));
            }

            if (digit.ValueRO.IsCritical != 0)
            {
                uniformScale *= 1.4f;
            }

            var position = startPos + localOffset;
            position.y = y;

            transform.ValueRW = LocalTransform.FromPositionRotationScale(
                position,
                BillboardRotation,
                uniformScale);

            var baseColor = color.ValueRO.Value;
            baseColor.w = alpha;
            color.ValueRW = new URPMaterialPropertyBaseColor { Value = baseColor };
        }
    }

    static float OutQuad(float t) => 1f - (1f - t) * (1f - t);

    static float InQuad(float t) => t * t;
}
