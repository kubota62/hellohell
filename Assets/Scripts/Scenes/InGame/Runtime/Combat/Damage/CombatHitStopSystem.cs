using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 戦闘ロジックから表示層へ渡す短時間のヒットストップ要求。
/// </summary>
public struct CombatHitStopRequest : IComponentData
{
    public float DurationSeconds;
    public float TimeScale;
}

/// <summary>
/// 会心とプレイヤー被弾だけを短く強調し、通常攻撃のテンポは維持する。
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CombatHitStopSystem : SystemBase
{
    float remainingSeconds;
    bool controlsTimeScale;

    protected override void OnUpdate()
    {
        var requestedDuration = 0f;
        var requestedTimeScale = 1f;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (request, entity) in
                 SystemAPI.Query<RefRO<CombatHitStopRequest>>()
                     .WithEntityAccess())
        {
            requestedDuration = math.max(
                requestedDuration,
                math.max(0f, request.ValueRO.DurationSeconds));
            requestedTimeScale = math.min(
                requestedTimeScale,
                math.clamp(request.ValueRO.TimeScale, 0.01f, 1f));
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        if (SystemAPI.TryGetSingleton<RunState>(out var runState))
        {
            if (runState.IsChoosingUpgrade != 0)
            {
                CancelHitStop(0f);
                return;
            }

            if (runState.IsGameOver != 0)
            {
                CancelHitStop(1f);
                return;
            }
        }

        if (requestedDuration > 0f && UnityEngine.Time.timeScale > 0f)
        {
            remainingSeconds = math.max(
                remainingSeconds,
                requestedDuration);
            UnityEngine.Time.timeScale = math.min(
                UnityEngine.Time.timeScale,
                requestedTimeScale);
            controlsTimeScale = true;
        }

        if (!controlsTimeScale)
        {
            return;
        }

        remainingSeconds = math.max(
            0f,
            remainingSeconds - UnityEngine.Time.unscaledDeltaTime);
        if (remainingSeconds <= 0f)
        {
            CancelHitStop(1f);
        }
    }

    protected override void OnDestroy()
    {
        if (controlsTimeScale && UnityEngine.Time.timeScale > 0f)
        {
            UnityEngine.Time.timeScale = 1f;
        }

        remainingSeconds = 0f;
        controlsTimeScale = false;
    }

    void CancelHitStop(float restoredTimeScale)
    {
        remainingSeconds = 0f;
        controlsTimeScale = false;
        UnityEngine.Time.timeScale = restoredTimeScale;
    }

    public static float ResolveDuration(
        int damage,
        bool isCritical,
        bool isPlayerDamage)
    {
        if (damage <= 0)
        {
            return 0f;
        }

        var damageAccent = math.sqrt(damage);
        if (isPlayerDamage)
        {
            return math.clamp(
                0.045f + damageAccent * 0.002f,
                0.045f,
                0.075f);
        }

        return isCritical
            ? math.clamp(
                0.018f + damageAccent * 0.0015f,
                0.018f,
                0.035f)
            : 0f;
    }

    public static float ResolveTimeScale(
        int damage,
        bool isCritical,
        bool isPlayerDamage)
    {
        if (damage <= 0)
        {
            return 1f;
        }

        if (isPlayerDamage)
        {
            return 0.06f;
        }

        return isCritical ? 0.16f : 1f;
    }
}
