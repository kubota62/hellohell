using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Pulls restorative shards toward the player and consumes them only when
/// health is missing, so full-health players do not waste recovery.
/// </summary>
[UpdateBefore(typeof(PlayerProgressSystem))]
public partial struct HealthPickupSystem : ISystem
{
    private const float BaseAttractionRadius = 2.75f;
    private const float AttractionRadiusPerLevel = 0.18f;
    private const float MaximumAttractionRadius = 14f;
    private const float MaximumDesperationAttractionRadius = 35f;
    private const float CollectRadius = 1.2f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<HealthPickup>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        if (!SystemAPI.HasComponent<LocalTransform>(playerEntity) ||
            !SystemAPI.HasComponent<Health>(playerEntity))
        {
            return;
        }

        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        playerPosition.y = 0.35f;
        var playerLevel = SystemAPI.HasComponent<PlayerProgress>(playerEntity)
            ? math.max(1, SystemAPI.GetComponent<PlayerProgress>(playerEntity).Level)
            : 1;
        var pickupRadiusAdd = SystemAPI.HasComponent<PlayerSkillStats>(playerEntity)
            ? math.max(
                0f,
                SystemAPI.GetComponent<PlayerSkillStats>(playerEntity).PickupRadiusAdd)
            : 0f;
        var attractionRadius = math.min(
            MaximumAttractionRadius,
            BaseAttractionRadius +
            (playerLevel - 1) * AttractionRadiusPerLevel +
            pickupRadiusAdd);
        var playerHealth = SystemAPI.GetComponent<Health>(playerEntity);
        attractionRadius = ResolveAttractionRadius(
            attractionRadius,
            playerHealth.Current,
            playerHealth.Max);
        var attractionRadiusSq = attractionRadius * attractionRadius;
        var deltaTime = SystemAPI.Time.DeltaTime;
        var totalHealed = 0;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, pickup, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<HealthPickup>>()
                     .WithEntityAccess())
        {
            var toPlayer = playerPosition - transform.ValueRO.Position;
            var distanceSq = math.lengthsq(toPlayer);
            if (distanceSq > attractionRadiusSq)
            {
                continue;
            }

            if (distanceSq <= CollectRadius * CollectRadius)
            {
                var healed = ApplyHealing(
                    ref playerHealth,
                    pickup.ValueRO.Healing);
                if (healed > 0)
                {
                    totalHealed += healed;
                    ecb.DestroyEntity(entity);
                }
                continue;
            }

            var distance = math.sqrt(distanceSq);
            var attraction = 1f - math.saturate(distance / attractionRadius);
            var speed = math.lerp(5f, 14f, attraction);
            transform.ValueRW.Position +=
                math.normalizesafe(toPlayer) * speed * deltaTime;
            transform.ValueRW.Rotation = math.mul(
                quaternion.RotateY(deltaTime * 6f),
                transform.ValueRO.Rotation);
        }

        if (totalHealed > 0)
        {
            ecb.SetComponent(playerEntity, playerHealth);
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent(eventEntity, new PlayerHealedEvent
            {
                Amount = totalHealed,
            });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static int ApplyHealing(ref Health health, int requestedHealing)
    {
        var before = math.clamp(health.Current, 0, math.max(1, health.Max));
        health.Max = math.max(1, health.Max);
        health.Current = math.min(
            health.Max,
            before + math.max(0, requestedHealing));
        return health.Current - before;
    }

    public static float ResolveAttractionMultiplier(
        int currentHealth,
        int maximumHealth)
    {
        if (currentHealth <= 0 || maximumHealth <= 0)
        {
            return 1f;
        }

        var ratio = math.saturate(
            (float)math.max(0, currentHealth) /
            math.max(1, maximumHealth));
        if (ratio <= 0.25f)
        {
            return 2.75f;
        }

        return ratio <= 0.5f ? 1.6f : 1f;
    }

    public static float ResolveAttractionRadius(
        float naturalRadius,
        int currentHealth,
        int maximumHealth)
    {
        return math.min(
            MaximumDesperationAttractionRadius,
            math.max(0f, naturalRadius) * ResolveAttractionMultiplier(
                currentHealth,
                maximumHealth));
    }
}

[UpdateBefore(typeof(ExperiencePickupSystem))]
public partial struct SlaughterMilestoneVacuumSystem : ISystem
{
    public const float PulseDurationSeconds = 3.5f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        var triggered = false;
        foreach (var _ in SystemAPI.Query<RefRO<SlaughterMilestoneEvent>>())
        {
            triggered = true;
        }

        var hasPulse = SystemAPI.HasComponent<PickupVacuumPulse>(playerEntity);
        var currentRemaining = hasPulse
            ? SystemAPI.GetComponent<PickupVacuumPulse>(playerEntity)
                .RemainingSeconds
            : 0f;
        var isPaused =
            SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 ||
             runState.IsChoosingUpgrade != 0);
        var remaining = ResolveRemainingSeconds(
            currentRemaining,
            triggered,
            SystemAPI.Time.DeltaTime,
            isPaused);

        if (remaining > 0f)
        {
            var pulse = new PickupVacuumPulse
            {
                RemainingSeconds = remaining,
            };
            if (hasPulse)
            {
                SystemAPI.SetComponent(playerEntity, pulse);
            }
            else
            {
                state.EntityManager.AddComponentData(playerEntity, pulse);
            }
        }
        else if (hasPulse)
        {
            state.EntityManager.RemoveComponent<PickupVacuumPulse>(
                playerEntity);
        }
    }

    public static float ResolveRemainingSeconds(
        float currentRemaining,
        bool triggered,
        float deltaTime,
        bool isPaused)
    {
        var safeCurrent = math.max(0f, currentRemaining);
        if (triggered)
        {
            return math.max(safeCurrent, PulseDurationSeconds);
        }

        return isPaused
            ? safeCurrent
            : math.max(0f, safeCurrent - math.max(0f, deltaTime));
    }
}

/// <summary>
/// 経験値ドロップをプレイヤーへ吸引し、接触時に報酬イベントへ変換する。
/// </summary>
[UpdateBefore(typeof(PlayerProgressSystem))]
public partial struct ExperiencePickupSystem : ISystem
{
    private const float BaseAttractionRadius = 2.75f;
    private const float AttractionRadiusPerLevel = 0.18f;
    private const float MaximumAttractionRadius = 14f;
    private const float CollectRadius = 1.2f;
    private const float MinimumMoveSpeed = 5f;
    private const float MaximumMoveSpeed = 14f;
    private const float VacuumAttractionRadius = 10000f;
    private const float VacuumMinimumMoveSpeed = 18f;
    private const float VacuumMaximumMoveSpeed = 30f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<ExperiencePickup>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        if (!SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            return;
        }

        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        playerPosition.y = 0.35f;
        var playerLevel = SystemAPI.HasComponent<PlayerProgress>(playerEntity)
            ? math.max(1, SystemAPI.GetComponent<PlayerProgress>(playerEntity).Level)
            : 1;
        var pickupRadiusAdd = SystemAPI.HasComponent<PlayerSkillStats>(playerEntity)
            ? math.max(
                0f,
                SystemAPI.GetComponent<PlayerSkillStats>(playerEntity).PickupRadiusAdd)
            : 0f;
        var attractionRadius = math.min(
            MaximumAttractionRadius,
            BaseAttractionRadius +
            (playerLevel - 1) * AttractionRadiusPerLevel +
            pickupRadiusAdd);
        var vacuumActive =
            SystemAPI.HasComponent<PickupVacuumPulse>(playerEntity) &&
            SystemAPI.GetComponent<PickupVacuumPulse>(playerEntity)
                .RemainingSeconds > 0f;
        attractionRadius = ResolveAttractionRadius(
            attractionRadius,
            vacuumActive);
        var attractionRadiusSq = attractionRadius * attractionRadius;
        var deltaTime = SystemAPI.Time.DeltaTime;
        var championPickupLookup =
            SystemAPI.GetComponentLookup<ChampionRewardPickup>(true);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, pickup, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<ExperiencePickup>>()
                     .WithEntityAccess())
        {
            var toPlayer = playerPosition - transform.ValueRO.Position;
            var distanceSq = math.lengthsq(toPlayer);
            if (distanceSq > attractionRadiusSq)
            {
                continue;
            }

            if (distanceSq <= CollectRadius * CollectRadius)
            {
                var rewardEvent = ecb.CreateEntity();
                ecb.AddComponent(rewardEvent, new EnemyRewardEvent
                {
                    EnemyTypeId = 0,
                    Experience = pickup.ValueRO.Experience,
                    Score = pickup.ValueRO.Score,
                });
                if (championPickupLookup.HasComponent(entity))
                {
                    var treasureEvent = ecb.CreateEntity();
                    var currentLevel = SystemAPI.HasComponent<PlayerProgress>(
                        playerEntity)
                            ? SystemAPI.GetComponent<PlayerProgress>(
                                playerEntity).Level
                            : 1;
                    ecb.AddComponent(
                        treasureEvent,
                        CreateChampionTreasureUpgradeEvent(
                            playerEntity,
                            currentLevel));
                }
                ecb.DestroyEntity(entity);
                continue;
            }

            var distance = math.sqrt(distanceSq);
            var attraction = 1f - math.saturate(distance / attractionRadius);
            var speed = ResolveAttractionSpeed(
                attraction,
                vacuumActive);
            transform.ValueRW.Position += math.normalizesafe(toPlayer) * speed * deltaTime;
            transform.ValueRW.Rotation = math.mul(
                quaternion.RotateY(deltaTime * 5f),
                transform.ValueRO.Rotation);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static float ResolveAttractionRadius(
        float naturalRadius,
        bool vacuumActive)
    {
        return vacuumActive
            ? VacuumAttractionRadius
            : math.max(0f, naturalRadius);
    }

    public static float ResolveAttractionSpeed(
        float attraction,
        bool vacuumActive)
    {
        var safeAttraction = math.saturate(attraction);
        return vacuumActive
            ? math.lerp(
                VacuumMinimumMoveSpeed,
                VacuumMaximumMoveSpeed,
                safeAttraction)
            : math.lerp(
                MinimumMoveSpeed,
                MaximumMoveSpeed,
                safeAttraction);
    }

    public static PlayerLevelUpEvent CreateChampionTreasureUpgradeEvent(
        Entity playerEntity,
        int currentLevel)
    {
        return new PlayerLevelUpEvent
        {
            Player = playerEntity,
            NewLevel = math.max(1, currentLevel),
            LevelsGained = 1,
            Source = PlayerUpgradeSource.ChampionTreasure,
        };
    }
}

/// <summary>
/// Player/Core/Progress の成長集計システム。
/// EnemyRewardEventをPlayerProgressへ加算する。
/// レベルアップ時は必要経験値を段階的に増やし、後続のスキル選択処理から参照できる状態にする。
/// </summary>
[BurstCompile]
public partial struct PlayerProgressSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerProgress>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var totalExperience = 0;
        var totalScore = 0;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (reward, eventEntity) in
                 SystemAPI.Query<RefRO<EnemyRewardEvent>>()
                     .WithEntityAccess())
        {
            totalExperience += reward.ValueRO.Experience;
            totalScore += reward.ValueRO.Score;
            ecb.DestroyEntity(eventEntity);
        }

        if (totalExperience <= 0 && totalScore <= 0)
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        var progressMaster = ResolveProgressMaster(ref state);
        foreach (var (progress, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerProgress>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            var experienceMultiplier =
                SystemAPI.HasComponent<PlayerSkillStats>(playerEntity)
                    ? PlayerAutoSkillSystem.GetExperienceMultiplier(
                        SystemAPI.GetComponent<PlayerSkillStats>(playerEntity))
                    : 1f;
            var value = progress.ValueRO;
            value.Level = math.max(1, value.Level);
            value.ExperienceToNextLevel = math.max(1, value.ExperienceToNextLevel);
            value.Experience += ResolveExperienceReward(
                totalExperience,
                experienceMultiplier);
            value.Score += totalScore;
            var levelsGained = 0;

            while (value.Experience >= value.ExperienceToNextLevel)
            {
                value.Experience -= value.ExperienceToNextLevel;
                value.Level++;
                levelsGained++;
                value.ExperienceToNextLevel = CalculateExperienceToNextLevel(value.Level, progressMaster);
            }

            progress.ValueRW = value;

            if (levelsGained > 0)
            {
                var eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new PlayerLevelUpEvent
                {
                    Player = playerEntity,
                    NewLevel = value.Level,
                    LevelsGained = levelsGained,
                    Source = PlayerUpgradeSource.LevelUp,
                });
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static int ResolveExperienceReward(
        int baseExperience,
        float multiplier)
    {
        if (baseExperience <= 0)
        {
            return 0;
        }

        return math.max(
            1,
            (int)math.round(
                baseExperience * math.clamp(multiplier, 1f, 3f)));
    }

    public static PlayerProgress CreateInitialProgress()
    {
        return CreateInitialProgress(PlayerProgressMasterCatalog.Get(PlayerProgressMasterId.Default));
    }

    public static PlayerProgress CreateInitialProgress(PlayerProgressMasterData master)
    {
        return new PlayerProgress
        {
            Level = math.max(1, master.InitialLevel),
            Experience = 0,
            ExperienceToNextLevel = math.max(1, master.InitialExperienceToNextLevel),
            Score = 0,
        };
    }

    private PlayerProgressMasterData ResolveProgressMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<PlayerProgressMasterElement>(out var masters, true))
        {
            return PlayerProgressMasterCatalog.Get(masters, PlayerProgressMasterId.Default);
        }

        return PlayerProgressMasterCatalog.Get(PlayerProgressMasterId.Default);
    }

    static int CalculateExperienceToNextLevel(int level, PlayerProgressMasterData master)
    {
        return math.max(1, master.InitialExperienceToNextLevel) +
            math.max(0, level - 1) * math.max(0, master.ExperienceToNextLevelAdd);
    }
}
