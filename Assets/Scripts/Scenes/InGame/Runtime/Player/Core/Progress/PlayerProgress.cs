using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Player/Core/Progress の共通コンポーネント。
/// Playerの成長状態とスコアを保持する。
/// 敵死亡時のEnemyRewardEventを集計し、将来のスキル選択やレベルアップ演出の入口にする。
/// </summary>
public struct PlayerProgress : IComponentData
{
    public int Level;
    public int Experience;
    public int ExperienceToNextLevel;
    public int Score;
}

public struct PlayerCombatStats : IComponentData
{
    public long TotalDamageDealt;
    public long WindowDamage;
    public float WindowElapsedSeconds;
    public float CurrentDps;
    public float PeakDps;
}

[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct PlayerCombatStatsSystem : ISystem
{
    public const float SampleWindowSeconds = 0.5f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var hasRunState = SystemAPI.TryGetSingleton<RunState>(
            out var runState);
        if (hasRunState && runState.IsChoosingUpgrade != 0)
        {
            return;
        }

        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var stats in
                 SystemAPI.Query<RefRW<PlayerCombatStats>>()
                     .WithAll<Player>())
        {
            var value = stats.ValueRO;
            if (hasRunState && runState.IsGameOver != 0)
            {
                FinalizePendingSample(ref value);
            }
            else
            {
                AdvanceSample(ref value, deltaTime);
            }

            stats.ValueRW = value;
        }
    }

    public static void RecordDamage(
        ref PlayerCombatStats stats,
        long damage)
    {
        var appliedDamage = math.max(0L, damage);
        stats.TotalDamageDealt += appliedDamage;
        stats.WindowDamage += appliedDamage;
    }

    public static void AdvanceSample(
        ref PlayerCombatStats stats,
        float deltaTime)
    {
        stats.WindowElapsedSeconds += math.max(0f, deltaTime);
        if (stats.WindowElapsedSeconds < SampleWindowSeconds)
        {
            return;
        }

        stats.CurrentDps = (float)stats.WindowDamage /
            math.max(SampleWindowSeconds, stats.WindowElapsedSeconds);
        stats.PeakDps = math.max(stats.PeakDps, stats.CurrentDps);
        stats.WindowDamage = 0;
        stats.WindowElapsedSeconds = 0f;
    }

    public static void FinalizePendingSample(ref PlayerCombatStats stats)
    {
        if (stats.WindowDamage <= 0)
        {
            return;
        }

        stats.CurrentDps = (float)stats.WindowDamage /
            math.max(SampleWindowSeconds, stats.WindowElapsedSeconds);
        stats.PeakDps = math.max(stats.PeakDps, stats.CurrentDps);
        stats.WindowDamage = 0;
        stats.WindowElapsedSeconds = 0f;
    }
}

/// <summary>
/// Enemies can leave a restorative shard behind. It is attracted by the
/// same pickup-range build stat as experience gems.
/// </summary>
public struct HealthPickup : IComponentData
{
    public int Healing;
}

/// <summary>One-frame feedback emitted after restorative shards heal the player.</summary>
public struct PlayerHealedEvent : IComponentData
{
    public int Amount;
}
