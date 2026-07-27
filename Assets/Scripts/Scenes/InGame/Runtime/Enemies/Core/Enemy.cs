using Unity.Entities;

/// <summary>
/// Enemies/Core の共通タグ。
/// エンティティを Enemy として扱うために付与する。
/// Team は敵対関係、Enemy は敵用システムで動く Actor かどうかを表す。
/// </summary>
public struct Enemy : IComponentData
{
}

/// <summary>
/// 通常敵より高い能力と報酬を持つ、ラン中の節目となる強敵。
/// </summary>
public struct EliteEnemy : IComponentData
{
}

/// <summary>
/// 一定時間ごとに出現する中ボス級の強敵。
/// EliteEnemyも併せて持ち、既存の強敵向け処理へ参加する。
/// </summary>
public struct ChampionEnemy : IComponentData
{
}

/// <summary>
/// ラン終盤に一度だけ出現し、撃破が勝利条件になる最終ボス。
/// ChampionEnemyとEliteEnemyも併せて持つ。
/// </summary>
public struct FinalBossEnemy : IComponentData
{
}

public struct FinalBossPhaseState : IComponentData
{
    public byte IsEnraged;
}

public struct FinalBossEnragedEvent : IComponentData
{
}

[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct FinalBossPhaseSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(
            Unity.Collections.Allocator.Temp);
        foreach (var (health, phase) in
                 SystemAPI.Query<RefRO<Health>, RefRW<FinalBossPhaseState>>()
                     .WithAll<FinalBossEnemy, GameplayActive>())
        {
            if (phase.ValueRO.IsEnraged != 0 ||
                !IsEnraged(
                    health.ValueRO.Current,
                    health.ValueRO.Max))
            {
                continue;
            }

            phase.ValueRW.IsEnraged = 1;
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent<FinalBossEnragedEvent>(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static bool IsEnraged(int currentHealth, int maxHealth)
    {
        return maxHealth > 0 &&
            currentHealth > 0 &&
            currentHealth <= maxHealth / 2;
    }
}
