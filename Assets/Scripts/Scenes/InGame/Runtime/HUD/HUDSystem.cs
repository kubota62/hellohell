using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// 現在生存している ActorBody、active な Projectile、PlayerProgressを集計し、Managed 側の HUD へ渡す。
/// inactive なプール待機弾は Projectile 数に含めない。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(PlayerAutoSkillSystem))]
public partial struct HUDSystem : ISystem
{
    EntityQuery actorCountQuery;
    EntityQuery projectileCountQuery;
    EntityQuery eliteCountQuery;
    EntityQuery championCountQuery;
    EntityQuery finalBossCountQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        actorCountQuery = SystemAPI.QueryBuilder().WithAll<ActorBody>().Build();
        projectileCountQuery = SystemAPI.QueryBuilder().WithAll<Projectile, GameplayActive>().Build();
        eliteCountQuery = SystemAPI.QueryBuilder()
            .WithAll<EliteEnemy, GameplayActive>()
            .WithNone<ChampionEnemy>()
            .Build();
        championCountQuery = SystemAPI.QueryBuilder()
            .WithAll<ChampionEnemy, GameplayActive>()
            .WithNone<FinalBossEnemy>()
            .Build();
        finalBossCountQuery = SystemAPI.QueryBuilder()
            .WithAll<FinalBossEnemy, GameplayActive>()
            .Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        var hudBridge = HUDBridge.Instance;
        if (hudBridge == null)
        {
            return;
        }

        hudBridge.SetActorCount(actorCountQuery.CalculateEntityCount());
        hudBridge.SetProjectileCount(projectileCountQuery.CalculateEntityCount());
        hudBridge.SetEliteCount(eliteCountQuery.CalculateEntityCount());
        ConsumeFeedbackEvents(ref state, hudBridge);

        if (SystemAPI.TryGetSingleton<PlayerProgress>(out var progress))
        {
            var health = new Health { Max = 1, Current = 1 };
            var skillStats = new PlayerSkillStats();
            if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
                SystemAPI.HasComponent<Health>(playerEntity))
            {
                health = SystemAPI.GetComponent<Health>(playerEntity);
                if (SystemAPI.HasComponent<PlayerSkillStats>(playerEntity))
                {
                    skillStats = SystemAPI.GetComponent<PlayerSkillStats>(playerEntity);
                }
            }

            var runState = SystemAPI.TryGetSingleton<RunState>(out var currentRun)
                ? currentRun
                : new RunState { ThreatLevel = 1 };
            var autoAttackEnabled =
                SystemAPI.TryGetSingleton<PlayerInput>(out var input) &&
                input.AutoAttackEnabled;

            hudBridge.SetPlayerProgress(
                progress.Level,
                progress.Experience,
                progress.ExperienceToNextLevel,
                progress.Score,
                health.Current,
                health.Max,
                runState.ElapsedSeconds,
                runState.DurationSeconds,
                runState.ThreatLevel,
                runState.IsGameOver != 0,
                runState.IsVictory != 0,
                autoAttackEnabled,
                skillStats);

            hudBridge.SetUpgradeChoice(
                SystemAPI.TryGetSingleton<PlayerUpgradeChoice>(out var upgradeChoice),
                upgradeChoice);
        }

        var championCurrentHealth = 0;
        var championMaxHealth = 0;
        foreach (var championHealth in
                 SystemAPI.Query<RefRO<Health>>()
                     .WithAll<ChampionEnemy, GameplayActive>()
                     .WithNone<FinalBossEnemy>())
        {
            championCurrentHealth += championHealth.ValueRO.Current;
            championMaxHealth += championHealth.ValueRO.Max;
        }

        var finalBossCurrentHealth = 0;
        var finalBossMaxHealth = 0;
        foreach (var finalBossHealth in
                 SystemAPI.Query<RefRO<Health>>()
                     .WithAll<FinalBossEnemy, GameplayActive>())
        {
            finalBossCurrentHealth += finalBossHealth.ValueRO.Current;
            finalBossMaxHealth += finalBossHealth.ValueRO.Max;
        }

        hudBridge.SetChampionState(
            championCountQuery.CalculateEntityCount(),
            championCurrentHealth,
            championMaxHealth,
            finalBossCountQuery.CalculateEntityCount(),
            finalBossCurrentHealth,
            finalBossMaxHealth);
        ConsumeFinalBossEvents(ref state, hudBridge);
    }

    private void ConsumeFeedbackEvents(
        ref SystemState state,
        HUDBridge hudBridge)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (skillEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerSkillAppliedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowSkillApplied(
                skillEvent.ValueRO.Kind,
                skillEvent.ValueRO.NewSkillLevel);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (defeatedEvent, eventEntity) in
                 SystemAPI.Query<RefRO<ChampionDefeatedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowChampionDefeated(
                defeatedEvent.ValueRO.Experience,
                defeatedEvent.ValueRO.Score);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (healedEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerHealedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowPlayerHealed(healedEvent.ValueRO.Amount);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (revivedEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerRevivedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowPlayerRevived(
                revivedEvent.ValueRO.RestoredHealth,
                revivedEvent.ValueRO.ChargesRemaining);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (surgeEvent, eventEntity) in
                 SystemAPI.Query<RefRO<HordeSurgeEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowHordeSurge(
                surgeEvent.ValueRO.Wave,
                surgeEvent.ValueRO.EnemyCount,
                surgeEvent.ValueRO.ThreatLevel);
            ecb.DestroyEntity(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void ConsumeFinalBossEvents(
        ref SystemState state,
        HUDBridge hudBridge)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (bossEvent, eventEntity) in
                 SystemAPI.Query<RefRO<FinalBossSpawnedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowFinalBossSpawned(
                bossEvent.ValueRO.ThreatLevel);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (_, eventEntity) in
                 SystemAPI.Query<RefRO<FinalBossEnragedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowFinalBossEnraged();
            ecb.DestroyEntity(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
