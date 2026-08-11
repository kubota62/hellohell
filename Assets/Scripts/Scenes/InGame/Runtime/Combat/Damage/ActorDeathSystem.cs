using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// 連続撃破が新しい血潮Tierへ到達した時のHUD通知。
/// </summary>
public struct KillStreakTierReachedEvent : IComponentData
{
    public int Tier;
    public int KillStreak;
}

/// <summary>
/// Health が 0 以下になった ActorBody をゲーム世界から除去するシステム。
/// Player のゲームオーバー処理は未設計のため、現時点では非 Player だけを破棄する。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct ActorDeathSystem : ISystem
{
    private const int MaximumExperiencePickups = 500;
    private const int MaximumHealthPickups = 60;
    private const int MaximumPowerShardPickups = 30;
    private const int MaximumDeathBurstParticles = 700;
    private const int MaximumVolatileHazards = 24;
    public const float KillStreakDurationSeconds = 3f;
    public const int SlaughterMilestoneInterval = 100;

    private EntityQuery experiencePickupQuery;
    private EntityQuery healthPickupQuery;
    private EntityQuery powerShardPickupQuery;
    private EntityQuery deathBurstQuery;
    private EntityQuery volatileHazardQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        experiencePickupQuery = SystemAPI.QueryBuilder()
            .WithAll<ExperiencePickup>()
            .Build();
        healthPickupQuery = SystemAPI.QueryBuilder()
            .WithAll<HealthPickup>()
            .Build();
        powerShardPickupQuery = SystemAPI.QueryBuilder()
            .WithAll<PowerShardPickup>()
            .Build();
        deathBurstQuery = SystemAPI.QueryBuilder()
            .WithAll<DeathBurstParticle>()
            .Build();
        volatileHazardQuery = SystemAPI.QueryBuilder()
            .WithAll<VolatileDeathHazard>()
            .Build();
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var rewardLookup = SystemAPI.GetComponentLookup<EnemyReward>(true);
        var eliteLookup = SystemAPI.GetComponentLookup<EliteEnemy>(true);
        var championLookup = SystemAPI.GetComponentLookup<ChampionEnemy>(true);
        var finalBossLookup =
            SystemAPI.GetComponentLookup<FinalBossEnemy>(true);
        var eliteAffixLookup =
            SystemAPI.GetComponentLookup<EliteAffix>(true);
        var enemyDamageMultiplierLookup =
            SystemAPI.GetComponentLookup<EnemyDamageMultiplier>(true);
        var enemyLookup = SystemAPI.GetComponentLookup<Enemy>(true);
        var colorLookup = SystemAPI.GetComponentLookup<
            URPMaterialPropertyBaseColor>(true);
        var config = SystemAPI.GetSingleton<Config>();
        var experiencePickupCount = experiencePickupQuery.CalculateEntityCount();
        var healthPickupCount = healthPickupQuery.CalculateEntityCount();
        var powerShardPickupCount =
            powerShardPickupQuery.CalculateEntityCount();
        var deathBurstCount = deathBurstQuery.CalculateEntityCount();
        var volatileHazardCount =
            volatileHazardQuery.CalculateEntityCount();
        var defeatedEnemyCount = 0;
        var hasRunState = SystemAPI.TryGetSingleton<RunState>(out var runState);

        foreach (var (health, transform, entity) in
                 SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                     .WithAll<ActorBody>()
                     .WithNone<Player>()
                     .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
            {
                continue;
            }

            var isEnemy = enemyLookup.HasComponent(entity);
            var isElite = eliteLookup.HasComponent(entity);
            var isChampion = championLookup.HasComponent(entity);
            var isFinalBoss = finalBossLookup.HasComponent(entity);
            var isVolatile =
                eliteAffixLookup.HasComponent(entity) &&
                eliteAffixLookup[entity].Kind == EliteAffixKind.Volatile;
            if (isEnemy)
            {
                defeatedEnemyCount++;
                if (hasRunState)
                {
                    var previousTier =
                        AttackRequestSystem.ResolveKillStreakMomentumTier(
                            runState.KillStreak);
                    RegisterKillStreak(
                        ref runState,
                        1,
                        KillStreakDurationSeconds);
                    var currentTier =
                        AttackRequestSystem.ResolveKillStreakMomentumTier(
                            runState.KillStreak);
                    if (currentTier > previousTier)
                    {
                        var tierEvent = ecb.CreateEntity();
                        ecb.AddComponent(
                            tierEvent,
                            new KillStreakTierReachedEvent
                            {
                                Tier = currentTier,
                                KillStreak = runState.KillStreak,
                            });
                    }
                }
            }

            if (rewardLookup.HasComponent(entity))
            {
                var reward = rewardLookup[entity];
                var pickupReward = ResolvePickupReward(
                    reward,
                    isFinalBoss);
                var scoreMultiplier = hasRunState
                    ? ResolveKillStreakScoreMultiplier(runState.KillStreak)
                    : 1;
                var experienceMultiplier = hasRunState
                    ? ResolveKillStreakExperienceMultiplier(
                        runState.KillStreak)
                    : 1f;
                pickupReward.Experience = ApplyExperienceMultiplier(
                    pickupReward.Experience,
                    experienceMultiplier);
                pickupReward.Score = ApplyScoreMultiplier(
                    pickupReward.Score,
                    scoreMultiplier);
                if (isFinalBoss)
                {
                    var immediateReward = ResolveImmediateReward(
                        reward,
                        isFinalBoss);
                    immediateReward.Score = ApplyScoreMultiplier(
                        immediateReward.Score,
                        scoreMultiplier);
                    CreateRewardEvent(
                        ecb,
                        immediateReward);
                }

                if (experiencePickupCount < MaximumExperiencePickups)
                {
                    CreateExperiencePickup(
                        ecb,
                        config.ProjectilePrefab,
                        transform.ValueRO.Position,
                        pickupReward,
                        isChampion);
                    experiencePickupCount++;
                }
                else
                {
                    CreateRewardEvent(ecb, pickupReward);
                }

                if (isChampion)
                {
                    var displayedReward = reward;
                    displayedReward.Experience =
                        pickupReward.Experience;
                    displayedReward.Score = ApplyScoreMultiplier(
                        displayedReward.Score,
                        scoreMultiplier);
                    CreateChampionDefeatedEvent(ecb, displayedReward);
                }

                var dropsHealth = isElite ||
                    isChampion ||
                    entity.Index % 18 == 0;
                if (dropsHealth && healthPickupCount < MaximumHealthPickups)
                {
                    CreateHealthPickup(
                        ecb,
                        config.ProjectilePrefab,
                        transform.ValueRO.Position,
                        isElite,
                        isChampion);
                    healthPickupCount++;
                }

                if (isElite &&
                    !isFinalBoss &&
                    powerShardPickupCount < MaximumPowerShardPickups)
                {
                    CreatePowerShardPickup(
                        ecb,
                        config.ProjectilePrefab,
                        transform.ValueRO.Position,
                        isChampion);
                    powerShardPickupCount++;
                }
            }

            if (isEnemy &&
                deathBurstCount < MaximumDeathBurstParticles)
            {
                var particleCount = math.min(
                    DeathBurstSpawnUtility.ResolveParticleCount(
                        isElite,
                        isChampion,
                        isFinalBoss),
                    MaximumDeathBurstParticles - deathBurstCount);
                DeathBurstSpawnUtility.Spawn(
                    ecb,
                    config.ProjectilePrefab,
                    transform.ValueRO.Position,
                    colorLookup.HasComponent(entity)
                        ? colorLookup[entity].Value
                        : new float4(0.72f, 0.72f, 0.72f, 1f),
                    entity.Index,
                    particleCount,
                    isElite,
                    isChampion,
                    isFinalBoss);
                deathBurstCount += particleCount;
            }

            if (isVolatile &&
                volatileHazardCount < MaximumVolatileHazards)
            {
                var damageMultiplier =
                    enemyDamageMultiplierLookup.HasComponent(entity)
                        ? enemyDamageMultiplierLookup[entity].Value
                        : 1f;
                VolatileDeathHazardUtility.Spawn(
                    state.EntityManager,
                    ecb,
                    config.ProjectilePrefab,
                    transform.ValueRO.Position,
                    VolatileDeathHazardUtility.ResolveDamage(
                        damageMultiplier));
                volatileHazardCount++;
            }

            ecb.DestroyEntity(entity);
        }

        if (defeatedEnemyCount > 0 && hasRunState)
        {
            var previousDefeatedEnemies = runState.EnemiesDefeated;
            runState.EnemiesDefeated = AccumulateDefeatedEnemies(
                runState.EnemiesDefeated,
                defeatedEnemyCount);
            CreateSlaughterMilestoneEvents(
                ecb,
                previousDefeatedEnemies,
                runState.EnemiesDefeated);
            SystemAPI.SetSingleton(runState);
        }
    }

    public static void RegisterKillStreak(
        ref RunState runState,
        int newlyDefeated,
        float durationSeconds)
    {
        if (newlyDefeated <= 0)
        {
            return;
        }

        runState.KillStreak = AccumulateDefeatedEnemies(
            runState.KillStreak,
            newlyDefeated);
        runState.BestKillStreak = math.max(
            math.max(0, runState.BestKillStreak),
            runState.KillStreak);
        runState.KillStreakRemainingSeconds = math.max(
            0.01f,
            durationSeconds);
    }

    public static int ResolveKillStreakScoreMultiplier(int killStreak)
    {
        return 1 + math.clamp(math.max(0, killStreak) / 10, 0, 2);
    }

    public static float ResolveKillStreakExperienceMultiplier(
        int killStreak)
    {
        return 1f +
            AttackRequestSystem.ResolveKillStreakMomentumTier(killStreak) *
            0.1f;
    }

    public static int ApplyExperienceMultiplier(
        int baseExperience,
        float multiplier)
    {
        var safeExperience = math.max(0, baseExperience);
        if (safeExperience <= 0)
        {
            return 0;
        }

        var scaledExperience =
            safeExperience * math.clamp(multiplier, 1f, 1.3f);
        return scaledExperience >= int.MaxValue
            ? int.MaxValue
            : math.max(1, (int)math.round(scaledExperience));
    }

    public static int ApplyScoreMultiplier(int baseScore, int multiplier)
    {
        var safeScore = math.max(0, baseScore);
        var safeMultiplier = math.clamp(multiplier, 1, 3);
        return safeScore > int.MaxValue / safeMultiplier
            ? int.MaxValue
            : safeScore * safeMultiplier;
    }

    public static int AccumulateDefeatedEnemies(
        int currentCount,
        int newlyDefeated)
    {
        var safeCurrent = math.max(0, currentCount);
        if (newlyDefeated <= 0)
        {
            return safeCurrent;
        }

        return newlyDefeated > int.MaxValue - safeCurrent
            ? int.MaxValue
            : safeCurrent + newlyDefeated;
    }

    public static int ResolveSlaughterMilestoneRank(int defeatedEnemies)
    {
        return math.max(0, defeatedEnemies) /
            SlaughterMilestoneInterval;
    }

    public static int ResolveSlaughterMilestoneExperience(int milestone)
    {
        var rank = ResolveSlaughterMilestoneRank(milestone);
        if (rank <= 0)
        {
            return 0;
        }

        return 20 + math.min(
            rank,
            (int.MaxValue - 20) / 5) * 5;
    }

    public static int ResolveSlaughterMilestoneScore(int milestone)
    {
        var rank = ResolveSlaughterMilestoneRank(milestone);
        if (rank <= 0)
        {
            return 0;
        }

        return math.min(rank, int.MaxValue / 500) * 500;
    }

    public static EnemyReward ResolvePickupReward(
        EnemyReward reward,
        bool isFinalBoss)
    {
        if (isFinalBoss)
        {
            reward.Score = 0;
        }

        return reward;
    }

    public static EnemyReward ResolveImmediateReward(
        EnemyReward reward,
        bool isFinalBoss)
    {
        return new EnemyReward
        {
            Experience = 0,
            Score = isFinalBoss
                ? math.max(0, reward.Score)
                : 0,
        };
    }

    private static void CreateHealthPickup(
        EntityCommandBuffer ecb,
        Entity projectilePrefab,
        float3 position,
        bool isElite,
        bool isChampion)
    {
        var healing = isChampion ? 40 : isElite ? 18 : 8;
        var pickupPosition = position + new float3(0.45f, 0.35f, 0f);
        var pickupScale = isChampion ? 0.75f : isElite ? 0.58f : 0.42f;
        var pickupEntity = ecb.Instantiate(projectilePrefab);
        ecb.SetComponent(pickupEntity, LocalTransform.FromPositionRotationScale(
            pickupPosition,
            quaternion.identity,
            pickupScale));
        ecb.SetComponent(pickupEntity, new URPMaterialPropertyBaseColor
        {
            Value = isChampion
                ? new float4(1f, 0.22f, 0.62f, 1f)
                : new float4(1f, 0.12f, 0.18f, 1f),
        });
        ecb.AddComponent(pickupEntity, new HealthPickup
        {
            Healing = healing,
        });
        ecb.AddComponent(
            pickupEntity,
            RewardPickupVisualSystem.Create(
                pickupPosition,
                pickupScale,
                isChampion,
                true));
        ecb.RemoveComponent<ProjectileMotion>(pickupEntity);
        ecb.RemoveComponent<Projectile>(pickupEntity);
        ecb.RemoveComponent<Lifetime>(pickupEntity);
        ecb.RemoveComponent<GameplayActive>(pickupEntity);
    }

    private static void CreatePowerShardPickup(
        EntityCommandBuffer ecb,
        Entity projectilePrefab,
        float3 position,
        bool isChampion)
    {
        var pickupPosition = position + new float3(-0.45f, 0.48f, 0f);
        var pickupScale = isChampion ? 0.95f : 0.7f;
        var pickupEntity = ecb.Instantiate(projectilePrefab);
        ecb.SetComponent(
            pickupEntity,
            LocalTransform.FromPositionRotationScale(
                pickupPosition,
                quaternion.identity,
                pickupScale));
        ecb.SetComponent(pickupEntity, new URPMaterialPropertyBaseColor
        {
            Value = isChampion
                ? new float4(0.78f, 0.22f, 1f, 1f)
                : new float4(0.08f, 0.72f, 1f, 1f),
        });
        ecb.AddComponent(
            pickupEntity,
            PowerShardBuffUtility.CreatePickup(isChampion));
        ecb.AddComponent(
            pickupEntity,
            RewardPickupVisualSystem.Create(
                pickupPosition,
                pickupScale,
                isChampion,
                false));
        ecb.RemoveComponent<ProjectileMotion>(pickupEntity);
        ecb.RemoveComponent<Projectile>(pickupEntity);
        ecb.RemoveComponent<Lifetime>(pickupEntity);
        ecb.RemoveComponent<GameplayActive>(pickupEntity);
    }

    private static void CreateSlaughterMilestoneEvents(
        EntityCommandBuffer ecb,
        int previousDefeatedEnemies,
        int currentDefeatedEnemies)
    {
        var previousRank = ResolveSlaughterMilestoneRank(
            previousDefeatedEnemies);
        var currentRank = ResolveSlaughterMilestoneRank(
            currentDefeatedEnemies);
        for (var rank = previousRank + 1; rank <= currentRank; rank++)
        {
            var milestone = rank * SlaughterMilestoneInterval;
            var experience = ResolveSlaughterMilestoneExperience(milestone);
            var score = ResolveSlaughterMilestoneScore(milestone);
            CreateRewardEvent(ecb, new EnemyReward
            {
                Experience = experience,
                Score = score,
            });

            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent(eventEntity, new SlaughterMilestoneEvent
            {
                Milestone = milestone,
                Experience = experience,
                Score = score,
            });
        }
    }

    private static void CreateExperiencePickup(
        EntityCommandBuffer ecb,
        Entity projectilePrefab,
        float3 position,
        EnemyReward reward,
        bool isChampion)
    {
        var pickupPosition = position + new float3(0f, 0.35f, 0f);
        var pickupScale = isChampion ? 0.85f : 0.45f;
        var pickupEntity = ecb.Instantiate(projectilePrefab);
        ecb.SetComponent(pickupEntity, LocalTransform.FromPositionRotationScale(
            pickupPosition,
            quaternion.identity,
            pickupScale));
        ecb.SetComponent(pickupEntity, new URPMaterialPropertyBaseColor
        {
            Value = isChampion
                ? new float4(1f, 0.68f, 0.08f, 1f)
                : new float4(0.2f, 1f, 0.35f, 1f),
        });
        ecb.AddComponent(pickupEntity, new ExperiencePickup
        {
            Experience = reward.Experience,
            Score = reward.Score,
        });
        ecb.AddComponent(
            pickupEntity,
            RewardPickupVisualSystem.Create(
                pickupPosition,
                pickupScale,
                isChampion,
                false));
        if (isChampion)
        {
            ecb.AddComponent<ChampionRewardPickup>(pickupEntity);
        }
        ecb.RemoveComponent<ProjectileMotion>(pickupEntity);
        ecb.RemoveComponent<Projectile>(pickupEntity);
        ecb.RemoveComponent<Lifetime>(pickupEntity);
        ecb.RemoveComponent<GameplayActive>(pickupEntity);
    }

    private static void CreateRewardEvent(
        EntityCommandBuffer ecb,
        EnemyReward reward)
    {
        var rewardEntity = ecb.CreateEntity();
        ecb.AddComponent(rewardEntity, new EnemyRewardEvent
        {
            EnemyTypeId = 0,
            Experience = reward.Experience,
            Score = reward.Score,
        });
    }

    private static void CreateChampionDefeatedEvent(
        EntityCommandBuffer ecb,
        EnemyReward reward)
    {
        var eventEntity = ecb.CreateEntity();
        ecb.AddComponent(eventEntity, new ChampionDefeatedEvent
        {
            Experience = reward.Experience,
            Score = reward.Score,
        });
    }
}

public struct SlaughterMilestoneEvent : IComponentData
{
    public int Milestone;
    public int Experience;
    public int Score;
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct SlaughterMilestoneEventCleanupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(
            Unity.Collections.Allocator.Temp);
        foreach (var (_, entity) in
                 SystemAPI.Query<RefRO<SlaughterMilestoneEvent>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>
/// 撃破が途切れた時に連続撃破をリセットする。
/// レベルアップ選択中と決着後はタイマーを停止する。
/// </summary>
[UpdateBefore(typeof(ActorDeathSystem))]
public partial struct KillStreakDecaySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RunState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var runState = SystemAPI.GetSingleton<RunState>();
        if (runState.KillStreak <= 0 ||
            runState.IsGameOver != 0 ||
            runState.IsChoosingUpgrade != 0)
        {
            return;
        }

        Advance(ref runState, SystemAPI.Time.DeltaTime);
        SystemAPI.SetSingleton(runState);
    }

    public static void Advance(ref RunState runState, float deltaTime)
    {
        runState.KillStreakRemainingSeconds -= math.max(0f, deltaTime);
        if (runState.KillStreakRemainingSeconds <= 0f)
        {
            runState.KillStreak = 0;
            runState.KillStreakRemainingSeconds = 0f;
        }
    }
}
