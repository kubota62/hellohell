using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
        ConsumeFeedbackEvents(ref state, hudBridge);

        if (SystemAPI.TryGetSingleton<PlayerProgress>(out var progress))
        {
            var health = new Health { Max = 1, Current = 1 };
            var skillStats = new PlayerSkillStats();
            var combatStats = new PlayerCombatStats();
            var powerShardBuff = new PowerShardBuff();
            if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
                SystemAPI.HasComponent<Health>(playerEntity))
            {
                health = SystemAPI.GetComponent<Health>(playerEntity);
                if (SystemAPI.HasComponent<PlayerSkillStats>(playerEntity))
                {
                    skillStats = SystemAPI.GetComponent<PlayerSkillStats>(
                        playerEntity);
                }
                if (SystemAPI.HasComponent<PlayerCombatStats>(playerEntity))
                {
                    combatStats = SystemAPI.GetComponent<PlayerCombatStats>(
                        playerEntity);
                }
                if (SystemAPI.HasComponent<PowerShardBuff>(playerEntity))
                {
                    powerShardBuff = SystemAPI.GetComponent<PowerShardBuff>(
                        playerEntity);
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
                runState.EnemiesDefeated,
                runState.KillStreak,
                runState.BestKillStreak,
                ActorDeathSystem.ResolveKillStreakScoreMultiplier(
                    runState.KillStreak),
                runState.KillStreakRemainingSeconds,
                combatStats.TotalDamageDealt,
                combatStats.CurrentDps,
                combatStats.PeakDps,
                runState.IsGameOver != 0,
                runState.IsVictory != 0,
                autoAttackEnabled,
                skillStats);
            hudBridge.SetPowerShardBuff(
                powerShardBuff.RemainingSeconds,
                powerShardBuff.DamageMultiplier,
                powerShardBuff.CooldownMultiplier);

            var upcomingThreat =
                ActorSpawnSystem.ResolveUpcomingThreatEvent(
                    runState.ElapsedSeconds,
                    out var upcomingThreatRemainingSeconds);
            hudBridge.SetUpcomingThreat(
                runState.IsGameOver == 0
                    ? upcomingThreat
                    : UpcomingThreatEventKind.None,
                upcomingThreatRemainingSeconds);

            hudBridge.SetUpgradeChoice(
                SystemAPI.TryGetSingleton<PlayerUpgradeChoice>(out var upgradeChoice),
                upgradeChoice);
        }

        var playerPosition = float3.zero;
        var hasPlayerPosition =
            SystemAPI.TryGetSingletonEntity<Player>(out var threatPlayerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(threatPlayerEntity);
        if (hasPlayerPosition)
        {
            playerPosition = SystemAPI
                .GetComponent<LocalTransform>(threatPlayerEntity)
                .Position;
        }

        var eliteCurrentHealth = 0;
        var eliteMaxHealth = 0;
        var eliteBarrierCurrent = 0f;
        var eliteBarrierMaximum = 0f;
        var nearestElitePosition = float3.zero;
        var nearestEliteDistanceSq = float.MaxValue;
        var hasElitePosition = false;
        var swiftEliteCount = 0;
        var juggernautEliteCount = 0;
        var volatileEliteCount = 0;
        var wardedEliteCount = 0;
        var regeneratingEliteCount = 0;
        var berserkerEliteCount = 0;
        var eliteAffixLookup =
            SystemAPI.GetComponentLookup<EliteAffix>(true);
        var eliteBarrierLookup =
            SystemAPI.GetComponentLookup<EliteBarrier>(true);
        foreach (var (eliteHealth, eliteTransform, eliteEntity) in
                 SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                     .WithAll<EliteEnemy, GameplayActive>()
                     .WithNone<ChampionEnemy>()
                     .WithEntityAccess())
        {
            eliteCurrentHealth += eliteHealth.ValueRO.Current;
            eliteMaxHealth += eliteHealth.ValueRO.Max;
            if (eliteBarrierLookup.HasComponent(eliteEntity))
            {
                var barrier = eliteBarrierLookup[eliteEntity];
                eliteBarrierCurrent += math.max(0f, barrier.Current);
                eliteBarrierMaximum += math.max(0f, barrier.Maximum);
            }
            if (eliteAffixLookup.HasComponent(eliteEntity))
            {
                var affix = eliteAffixLookup[eliteEntity].Kind;
                swiftEliteCount += affix == EliteAffixKind.Swift ? 1 : 0;
                juggernautEliteCount +=
                    affix == EliteAffixKind.Juggernaut ? 1 : 0;
                volatileEliteCount +=
                    affix == EliteAffixKind.Volatile ? 1 : 0;
                wardedEliteCount +=
                    affix == EliteAffixKind.Warded ? 1 : 0;
                regeneratingEliteCount +=
                    affix == EliteAffixKind.Regenerating ? 1 : 0;
                berserkerEliteCount +=
                    affix == EliteAffixKind.Berserker ? 1 : 0;
            }
            SelectNearestThreat(
                playerPosition,
                eliteTransform.ValueRO.Position,
                ref hasElitePosition,
                ref nearestEliteDistanceSq,
                ref nearestElitePosition);
        }

        hudBridge.SetEliteCount(
            eliteCountQuery.CalculateEntityCount(),
            swiftEliteCount,
            juggernautEliteCount,
            volatileEliteCount,
            wardedEliteCount,
            regeneratingEliteCount,
            berserkerEliteCount);

        var championCurrentHealth = 0;
        var championMaxHealth = 0;
        var nearestChampionPosition = float3.zero;
        var nearestChampionDistanceSq = float.MaxValue;
        var hasChampionPosition = false;
        foreach (var (championHealth, championTransform) in
                 SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                     .WithAll<ChampionEnemy, GameplayActive>()
                     .WithNone<FinalBossEnemy>())
        {
            championCurrentHealth += championHealth.ValueRO.Current;
            championMaxHealth += championHealth.ValueRO.Max;
            SelectNearestThreat(
                playerPosition,
                championTransform.ValueRO.Position,
                ref hasChampionPosition,
                ref nearestChampionDistanceSq,
                ref nearestChampionPosition);
        }

        var finalBossCurrentHealth = 0;
        var finalBossMaxHealth = 0;
        var nearestFinalBossPosition = float3.zero;
        var nearestFinalBossDistanceSq = float.MaxValue;
        var hasFinalBossPosition = false;
        foreach (var (finalBossHealth, finalBossTransform) in
                 SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                     .WithAll<FinalBossEnemy, GameplayActive>())
        {
            finalBossCurrentHealth += finalBossHealth.ValueRO.Current;
            finalBossMaxHealth += finalBossHealth.ValueRO.Max;
            SelectNearestThreat(
                playerPosition,
                finalBossTransform.ValueRO.Position,
                ref hasFinalBossPosition,
                ref nearestFinalBossDistanceSq,
                ref nearestFinalBossPosition);
        }

        hudBridge.SetChampionState(
            eliteCurrentHealth,
            eliteMaxHealth,
            eliteBarrierCurrent,
            eliteBarrierMaximum,
            championCountQuery.CalculateEntityCount(),
            championCurrentHealth,
            championMaxHealth,
            finalBossCountQuery.CalculateEntityCount(),
            finalBossCurrentHealth,
            finalBossMaxHealth);
        if (hasPlayerPosition && hasFinalBossPosition)
        {
            hudBridge.SetThreatIndicator(true, 3, nearestFinalBossPosition);
        }
        else if (hasPlayerPosition && hasChampionPosition)
        {
            hudBridge.SetThreatIndicator(true, 2, nearestChampionPosition);
        }
        else if (hasPlayerPosition && hasElitePosition)
        {
            hudBridge.SetThreatIndicator(true, 1, nearestElitePosition);
        }
        else
        {
            hudBridge.SetThreatIndicator(false, 0, float3.zero);
        }

        ConsumeFinalBossEvents(ref state, hudBridge);
    }

    static void SelectNearestThreat(
        float3 playerPosition,
        float3 candidatePosition,
        ref bool hasSelection,
        ref float nearestDistanceSq,
        ref float3 nearestPosition)
    {
        var distanceSq = math.distancesq(playerPosition, candidatePosition);
        if (hasSelection && distanceSq >= nearestDistanceSq)
        {
            return;
        }

        hasSelection = true;
        nearestDistanceSq = distanceSq;
        nearestPosition = candidatePosition;
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
                skillEvent.ValueRO.NewSkillLevel,
                skillEvent.ValueRO.Quality);
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

        foreach (var (milestoneEvent, eventEntity) in
                 SystemAPI.Query<RefRO<SlaughterMilestoneEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowSlaughterMilestone(
                milestoneEvent.ValueRO.Milestone,
                milestoneEvent.ValueRO.Experience,
                milestoneEvent.ValueRO.Score);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (tierEvent, eventEntity) in
                 SystemAPI.Query<RefRO<KillStreakTierReachedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowKillStreakTierReached(
                tierEvent.ValueRO.Tier,
                tierEvent.ValueRO.KillStreak);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (healedEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerHealedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowPlayerHealed(healedEvent.ValueRO.Amount);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (shardEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PowerShardCollectedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowPowerShardCollected(
                shardEvent.ValueRO.DurationSeconds,
                shardEvent.ValueRO.DamageMultiplier,
                shardEvent.ValueRO.CooldownMultiplier);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (_, eventEntity) in
                 SystemAPI.Query<RefRO<EliteBarrierBrokenEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowEliteBarrierBroken();
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

        foreach (var (warningEvent, eventEntity) in
                 SystemAPI.Query<RefRO<HordeSurgeWarningEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowHordeWarning(
                warningEvent.ValueRO.Wave,
                warningEvent.ValueRO.SecondsUntilArrival,
                warningEvent.ValueRO.ThreatLevel);
            ecb.DestroyEntity(eventEntity);
        }

        foreach (var (warningEvent, eventEntity) in
                 SystemAPI.Query<RefRO<ChampionWaveWarningEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowChampionWarning(
                warningEvent.ValueRO.Wave,
                warningEvent.ValueRO.SecondsUntilArrival,
                warningEvent.ValueRO.ThreatLevel,
                warningEvent.ValueRO.IncludesHorde != 0);
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
        foreach (var (_, eventEntity) in
                 SystemAPI.Query<RefRO<ChampionEnragedEvent>>()
                     .WithEntityAccess())
        {
            hudBridge.ShowChampionEnraged();
            ecb.DestroyEntity(eventEntity);
        }

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
