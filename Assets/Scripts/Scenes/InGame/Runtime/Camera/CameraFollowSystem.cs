using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// CameraTarget を持つ Actor を追従するメインカメラ制御。
/// Entity の移動が終わった後に GameObject の Camera を更新するため、PresentationSystemGroup で実行する。
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{
    private const float CameraHeight = 18f;
    private const float CameraBackOffset = 8f;
    private float shakeAmplitude;
    private float shakeDuration;
    private float shakeRemaining;
    private float shakeTime;

    protected override void OnCreate()
    {
        RequireForUpdate<CameraTarget>();
    }

    protected override void OnUpdate()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (request, entity) in
                 SystemAPI.Query<RefRO<CameraShakeRequest>>()
                     .WithEntityAccess())
        {
            var amplitude = math.max(0f, request.ValueRO.Amplitude);
            var duration = math.max(0f, request.ValueRO.DurationSeconds);
            if (amplitude >= shakeAmplitude * ResolveShakeFade(
                    shakeRemaining,
                    shakeDuration))
            {
                shakeAmplitude = amplitude;
                shakeDuration = duration;
                shakeTime = 0f;
            }

            shakeRemaining = math.max(shakeRemaining, duration);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CameraTarget>())
        {
            var targetPos = transform.ValueRO.Position;
            var cameraPosition = new Vector3(
                targetPos.x,
                targetPos.y + CameraHeight,
                targetPos.z - CameraBackOffset);
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (shakeRemaining > 0f)
            {
                shakeTime += deltaTime;
                shakeRemaining = math.max(0f, shakeRemaining - deltaTime);
                var fade = ResolveShakeFade(shakeRemaining, shakeDuration);
                var offset = ResolveShakeOffset(
                    shakeTime,
                    shakeAmplitude,
                    fade);
                cameraPosition += new Vector3(offset.x, offset.y, 0f);
            }

            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(new Vector3(
                targetPos.x,
                targetPos.y,
                targetPos.z));
            break;
        }
    }

    public static float ResolveShakeFade(float remaining, float duration)
    {
        return math.saturate(remaining / math.max(0.01f, duration));
    }

    public static float2 ResolveShakeOffset(
        float elapsed,
        float amplitude,
        float fade)
    {
        var safeAmplitude = math.max(0f, amplitude) * math.saturate(fade);
        return new float2(
            math.sin(elapsed * 71f) * safeAmplitude,
            math.sin(elapsed * 97f + 1.37f) * safeAmplitude * 0.65f);
    }
}

/// <summary>
/// 戦闘ロジックからカメラ演出へ渡す、一フレーム限りの振動要求。
/// </summary>
public struct CameraShakeRequest : IComponentData
{
    public float Amplitude;
    public float DurationSeconds;
}
