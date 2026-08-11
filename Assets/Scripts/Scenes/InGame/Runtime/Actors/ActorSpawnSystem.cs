using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public enum UpcomingThreatEventKind : byte
{
    None,
    HordeSurge,
    ChampionWave,
    HordeAndChampion,
    FinalBoss,
}

/// <summary>
/// 共通Actor PrefabからPlayerとEnemyを生成し、マスター設定を適用する。
/// </summary>
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ActorSpawnSystem : ISystem
{
    private const int MaximumActiveEnemies = 350;
    public const float ChampionSpawnIntervalSeconds = 90f;
    public const float HordeSurgeIntervalSeconds = 60f;
    private const float HordeWarningLeadTime = 3f;
    private const float ChampionWarningLeadTime = 3f;
    public const float FinalBossSpawnTimeSeconds = 540f;
    private const float RunDurationSeconds = 600f;

    private Random random;
    private int spawnedActorCount;
    private int championWavesSpawned;
    private int championWarningsIssued;
    private int hordeWavesSpawned;
    private int hordeWarningsIssued;
    private bool finalBossSpawned;
    private float enemySpawnTimer;
    private EntityQuery activeEnemyQuery;
    private EntityQuery activeFinalBossQuery;

    public void OnCreate(ref SystemState state)
    {
        random = new Random(123);
        spawnedActorCount = 0;
        championWavesSpawned = 0;
        championWarningsIssued = 0;
        hordeWavesSpawned = 0;
        hordeWarningsIssued = 0;
        finalBossSpawned = false;
        enemySpawnTimer = 0f;
        activeEnemyQuery = SystemAPI.QueryBuilder()
            .WithAll<Enemy, GameplayActive>()
            .Build();
        activeFinalBossQuery = SystemAPI.QueryBuilder()
            .WithAll<FinalBossEnemy, GameplayActive>()
            .Build();

        var runStateEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(runStateEntity, new RunState
        {
            DurationSeconds = RunDurationSeconds,
            ThreatLevel = 1,
        });

        state.RequireForUpdate<Config>();
        state.RequireForUpdate<RunState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var runState = SystemAPI.GetSingleton<RunState>();
        if (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0)
        {
            return;
        }

        runState.ElapsedSeconds += SystemAPI.Time.DeltaTime;
        runState.ThreatLevel = 1 +
            (int)math.floor(runState.ElapsedSeconds / 30f);

        if (spawnedActorCount == 0)
        {
            SystemAPI.SetSingleton(runState);
            SpawnActor(
                ref state,
                true,
                float3.zero,
                runState.ElapsedSeconds,
                runState.ThreatLevel);
            spawnedActorCount = 1;
            enemySpawnTimer = -ResolveSpawnMaster(ref state).InitialEnemySpawnDelay;
            return;
        }

        var spawnMaster = ResolveSpawnMaster(ref state);
        enemySpawnTimer += SystemAPI.Time.DeltaTime;
        var availableSlots = MaximumActiveEnemies -
            activeEnemyQuery.CalculateEntityCount();
        var warningWave = ResolveHordeWarningWave(
            runState.ElapsedSeconds,
            hordeWarningsIssued);
        if (warningWave > 0)
        {
            hordeWarningsIssued = warningWave;
            var warningEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(
                warningEntity,
                new HordeSurgeWarningEvent
                {
                    Wave = warningWave,
                    SecondsUntilArrival = math.max(
                        0f,
                        warningWave * HordeSurgeIntervalSeconds -
                            runState.ElapsedSeconds),
                    ThreatLevel = runState.ThreatLevel,
                });
        }

        var championWarningWave = ResolveChampionWarningWave(
            runState.ElapsedSeconds,
            championWarningsIssued);
        if (championWarningWave > 0)
        {
            championWarningsIssued = championWarningWave;
            var warningEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(
                warningEntity,
                new ChampionWaveWarningEvent
                {
                    Wave = championWarningWave,
                    SecondsUntilArrival = math.max(
                        0f,
                        championWarningWave * ChampionSpawnIntervalSeconds -
                            runState.ElapsedSeconds),
                    ThreatLevel = runState.ThreatLevel,
                    IncludesHorde = IsCombinedThreatWave(
                            championWarningWave)
                        ? (byte)1
                        : (byte)0,
                });
        }

        if (ShouldSpawnFinalBoss(
                runState.ElapsedSeconds,
                finalBossSpawned))
        {
            SpawnActor(
                ref state,
                false,
                GetEnemySpawnPosition(ref state, spawnMaster),
                runState.ElapsedSeconds,
                runState.ThreatLevel,
                isElite: true,
                isChampion: true,
                isFinalBoss: true);
            spawnedActorCount++;
            runState.EnemiesSpawned++;
            finalBossSpawned = true;
            championWavesSpawned = math.max(
                championWavesSpawned,
                (int)math.floor(
                    runState.ElapsedSeconds / ChampionSpawnIntervalSeconds));
            championWarningsIssued = math.max(
                championWarningsIssued,
                championWavesSpawned);
            hordeWavesSpawned = math.max(
                hordeWavesSpawned,
                (int)math.floor(
                    runState.ElapsedSeconds / HordeSurgeIntervalSeconds));
            hordeWarningsIssued = math.max(
                hordeWarningsIssued,
                hordeWavesSpawned);

            var eventEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(
                eventEntity,
                new FinalBossSpawnedEvent
                {
                    ThreatLevel = runState.ThreatLevel,
                });
            enemySpawnTimer = 0f;
            SystemAPI.SetSingleton(runState);
            return;
        }

        if (runState.ElapsedSeconds >= runState.DurationSeconds)
        {
            runState.ElapsedSeconds = runState.DurationSeconds;
            if (CanClaimVictory(
                    runState.ElapsedSeconds,
                    runState.DurationSeconds,
                    finalBossSpawned,
                    activeFinalBossQuery.CalculateEntityCount()))
            {
                runState.IsVictory = 1;
                runState.IsGameOver = 1;
            }

            SystemAPI.SetSingleton(runState);
            return;
        }

        var championWave = (int)math.floor(
            runState.ElapsedSeconds / ChampionSpawnIntervalSeconds);
        if (championWave > championWavesSpawned && availableSlots > 0)
        {
            var championCount = CalculateChampionCount(
                championWave,
                availableSlots);
            for (var i = 0; i < championCount; i++)
            {
                SpawnActor(
                    ref state,
                    false,
                    GetEnemySpawnPosition(ref state, spawnMaster),
                    runState.ElapsedSeconds,
                    runState.ThreatLevel,
                    isElite: true,
                    isChampion: true);
                spawnedActorCount++;
                runState.EnemiesSpawned++;
            }

            championWavesSpawned = championWave;
            enemySpawnTimer = 0f;
            SystemAPI.SetSingleton(runState);
            return;
        }

        var hordeWave = (int)math.floor(
            runState.ElapsedSeconds / HordeSurgeIntervalSeconds);
        if (hordeWave > hordeWavesSpawned && availableSlots > 0)
        {
            var hordeSize = CalculateHordeSize(
                runState.ThreatLevel,
                availableSlots);
            SpawnHordeSurge(
                ref state,
                spawnMaster,
                ref runState,
                hordeWave,
                hordeSize);
            hordeWavesSpawned = hordeWave;
            hordeWarningsIssued = math.max(
                hordeWarningsIssued,
                hordeWave);
            enemySpawnTimer = 0f;
            SystemAPI.SetSingleton(runState);
            return;
        }

        var spawnInterval = math.max(
            0.12f,
            spawnMaster.SpawnInterval /
            (1f + runState.ElapsedSeconds / 120f));
        if (enemySpawnTimer <= spawnInterval)
        {
            SystemAPI.SetSingleton(runState);
            return;
        }

        if (availableSlots <= 0)
        {
            enemySpawnTimer = 0f;
            SystemAPI.SetSingleton(runState);
            return;
        }

        var batchSize = math.min(
            availableSlots,
            math.min(4, 1 + (runState.ThreatLevel - 1) / 3));
        for (var i = 0; i < batchSize; i++)
        {
            var nextEnemyOrdinal = runState.EnemiesSpawned + 1;
            var isElite = runState.ThreatLevel >= 2 &&
                nextEnemyOrdinal % 25 == 0;
            SpawnActor(
                ref state,
                false,
                GetEnemySpawnPosition(ref state, spawnMaster),
                runState.ElapsedSeconds,
                runState.ThreatLevel,
                isElite);
            spawnedActorCount++;
            runState.EnemiesSpawned++;
        }

        enemySpawnTimer = 0f;
        SystemAPI.SetSingleton(runState);
    }

    public static int CalculateHordeSize(int threatLevel, int availableSlots)
    {
        var desiredSize = 10 + math.clamp(threatLevel, 1, 14);
        return math.clamp(desiredSize, 0, math.max(0, availableSlots));
    }

    public static UpcomingThreatEventKind ResolveUpcomingThreatEvent(
        float elapsedSeconds,
        out float remainingSeconds)
    {
        var safeElapsedSeconds = math.max(0f, elapsedSeconds);
        if (safeElapsedSeconds >= FinalBossSpawnTimeSeconds)
        {
            remainingSeconds = 0f;
            return UpcomingThreatEventKind.None;
        }

        var nextHordeTime =
            (math.floor(safeElapsedSeconds / HordeSurgeIntervalSeconds) + 1f) *
            HordeSurgeIntervalSeconds;
        var nextChampionTime =
            (math.floor(
                safeElapsedSeconds / ChampionSpawnIntervalSeconds) + 1f) *
            ChampionSpawnIntervalSeconds;
        var eventTime = math.min(
            math.min(nextHordeTime, nextChampionTime),
            FinalBossSpawnTimeSeconds);
        remainingSeconds = math.max(
            0f,
            eventTime - safeElapsedSeconds);
        if (eventTime >= FinalBossSpawnTimeSeconds)
        {
            return UpcomingThreatEventKind.FinalBoss;
        }

        var isHorde = math.abs(nextHordeTime - eventTime) < 0.01f;
        var isChampion = math.abs(nextChampionTime - eventTime) < 0.01f;
        if (isHorde && isChampion)
        {
            return UpcomingThreatEventKind.HordeAndChampion;
        }

        return isChampion
            ? UpcomingThreatEventKind.ChampionWave
            : UpcomingThreatEventKind.HordeSurge;
    }

    public static int ResolveHordeWarningWave(
        float elapsedSeconds,
        int warningsIssued)
    {
        var safeElapsed = math.max(0f, elapsedSeconds);
        var candidateWave = (int)math.floor(
            (safeElapsed + HordeWarningLeadTime) /
            HordeSurgeIntervalSeconds);
        if (candidateWave <= math.max(0, warningsIssued))
        {
            return 0;
        }

        var arrivalTime = candidateWave * HordeSurgeIntervalSeconds;
        if (arrivalTime >= FinalBossSpawnTimeSeconds ||
            safeElapsed < arrivalTime - HordeWarningLeadTime ||
            safeElapsed >= arrivalTime)
        {
            return 0;
        }

        return candidateWave;
    }

    public static int CalculateChampionCount(int wave, int availableSlots)
    {
        var desiredCount = math.clamp(
            1 + math.max(0, wave - 1) / 2,
            1,
            3);
        return math.clamp(
            desiredCount,
            0,
            math.max(0, availableSlots));
    }

    public static int ResolveChampionWarningWave(
        float elapsedSeconds,
        int warningsIssued)
    {
        var safeElapsed = math.max(0f, elapsedSeconds);
        var candidateWave = (int)math.floor(
            (safeElapsed + ChampionWarningLeadTime) /
            ChampionSpawnIntervalSeconds);
        if (candidateWave <= math.max(0, warningsIssued))
        {
            return 0;
        }

        var arrivalTime = candidateWave * ChampionSpawnIntervalSeconds;
        if (arrivalTime >= FinalBossSpawnTimeSeconds ||
            safeElapsed < arrivalTime - ChampionWarningLeadTime ||
            safeElapsed >= arrivalTime)
        {
            return 0;
        }

        return candidateWave;
    }

    public static bool IsCombinedThreatWave(int championWave)
    {
        if (championWave <= 0)
        {
            return false;
        }

        var arrivalTime = championWave * ChampionSpawnIntervalSeconds;
        return math.abs(
            math.fmod(arrivalTime, HordeSurgeIntervalSeconds)) < 0.01f;
    }

    public static bool ShouldSpawnFinalBoss(
        float elapsedSeconds,
        bool hasSpawned)
    {
        return !hasSpawned &&
            elapsedSeconds >= FinalBossSpawnTimeSeconds;
    }

    public static bool CanClaimVictory(
        float elapsedSeconds,
        float durationSeconds,
        bool hasSpawnedFinalBoss,
        int activeFinalBossCount)
    {
        return elapsedSeconds >= math.max(0f, durationSeconds) &&
            hasSpawnedFinalBoss &&
            activeFinalBossCount <= 0;
    }

    private void SpawnHordeSurge(
        ref SystemState state,
        in SpawnMasterData spawnMaster,
        ref RunState runState,
        int wave,
        int hordeSize)
    {
        if (hordeSize <= 0)
        {
            return;
        }

        var playerPosition = GetPlayerPosition(ref state);
        var phase = random.NextFloat(0f, 2f * math.PI);
        var ringDistance = math.lerp(
            spawnMaster.MinSpawnDistance,
            spawnMaster.MaxSpawnDistance,
            0.68f);

        for (var i = 0; i < hordeSize; i++)
        {
            var angle = phase +
                (2f * math.PI * i / math.max(1, hordeSize));
            var radialJitter = random.NextFloat(-1.5f, 1.5f);
            var position = playerPosition +
                new float3(math.cos(angle), 0f, math.sin(angle)) *
                (ringDistance + radialJitter);
            var isElite = runState.ThreatLevel >= 7 &&
                (i + 1) % 8 == 0;

            SpawnActor(
                ref state,
                false,
                position,
                runState.ElapsedSeconds,
                runState.ThreatLevel,
                isElite);
            spawnedActorCount++;
            runState.EnemiesSpawned++;
        }

        var eventEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(eventEntity, new HordeSurgeEvent
        {
            Wave = wave,
            EnemyCount = hordeSize,
            ThreatLevel = runState.ThreatLevel,
        });
    }

    private void SpawnActor(
        ref SystemState state,
        bool isPlayer,
        float3 position,
        float elapsedSeconds,
        int threatLevel,
        bool isElite = false,
        bool isChampion = false,
        bool isFinalBoss = false)
    {
        var entityManager = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();
        var actorEntity = entityManager.Instantiate(config.ActorPrefab);
        var rotation = quaternion.RotateY(random.NextFloat(0f, 2f * math.PI));

        entityManager.SetComponentData(
            actorEntity,
            LocalTransform.FromPositionRotation(position, rotation));
        SetOrAddComponent(entityManager, actorEntity, new MoveIntent());

        if (isPlayer)
        {
            ConfigurePlayer(
                entityManager,
                actorEntity,
                ResolvePlayerMaster(ref state),
                ResolvePlayerProgressMaster(ref state));
        }
        else
        {
            var eliteAffix = EliteAffixUtility.ResolveForSpawn(
                spawnedActorCount,
                isElite,
                isChampion,
                isFinalBoss);
            ConfigureEnemy(
                entityManager,
                actorEntity,
                ResolveEnemyMaster(ref state, threatLevel),
                elapsedSeconds,
                isElite,
                isChampion,
                isFinalBoss,
                eliteAffix);
            ThreatAuraSpawnUtility.Spawn(
                entityManager,
                config.ProjectilePrefab,
                actorEntity,
                isElite,
                isChampion,
                isFinalBoss,
                eliteAffix);
        }

        ApplyActorColorToChildren(entityManager, actorEntity);
    }

    private EnemyMasterData ResolveEnemyMaster(
        ref SystemState state,
        int threatLevel)
    {
        var enemySpawnIndex = spawnedActorCount - 1;
        var playerLevel = SystemAPI.TryGetSingleton<PlayerProgress>(out var progress)
            ? progress.Level
            : 1;
        var unlockLevel = math.max(playerLevel, threatLevel);

        if (SystemAPI.TryGetSingletonBuffer<EnemyMasterElement>(
                out var enemyMasters,
                true))
        {
            return EnemyMasterCatalog.PickSpawnMaster(
                enemyMasters,
                enemySpawnIndex,
                unlockLevel);
        }

        return EnemyMasterCatalog.Get(
            EnemyMasterCatalog.PickSpawnType(enemySpawnIndex));
    }

    private static void ConfigurePlayer(
        EntityManager entityManager,
        Entity actorEntity,
        PlayerMasterData playerMaster,
        PlayerProgressMasterData progressMaster)
    {
        EnsureTag<Player>(entityManager, actorEntity);
        EnsureTag<CameraTarget>(entityManager, actorEntity);

        SetOrAddComponent(
            entityManager,
            actorEntity,
            new Team { Value = TeamId.Player });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new AttackLoadout { PrimaryAttack = playerMaster.PrimaryAttack });
        SetOrAddComponent(entityManager, actorEntity, new AttackCooldown());
        SetOrAddComponent(
            entityManager,
            actorEntity,
            PlayerProgressSystem.CreateInitialProgress(progressMaster));
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new PlayerCombatStats());
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new PlayerSkillStats
            {
                // A run begins with one signature weapon. The remaining slots
                // stay present but are unlocked through level-up choices.
                MeleeArcLevel = 1,
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new URPMaterialPropertyBaseColor
            {
                Value = new float4(1f, 1f, 1f, 1f),
            });

        var attackSlots = entityManager.HasBuffer<PlayerAttackSlot>(actorEntity)
            ? entityManager.GetBuffer<PlayerAttackSlot>(actorEntity)
            : entityManager.AddBuffer<PlayerAttackSlot>(actorEntity);
        attackSlots.Clear();

        for (var i = 0; i < AttackMasterIdUtility.PlayerDefaults.Length; i++)
        {
            attackSlots.Add(new PlayerAttackSlot
            {
                AttackMasterId = AttackMasterIdUtility.PlayerDefaults[i],
                Remaining = 0f,
            });
        }
    }

    private static void ConfigureEnemy(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyMasterData definition,
        float elapsedSeconds,
        bool isElite,
        bool isChampion,
        bool isFinalBoss,
        EliteAffixKind eliteAffix)
    {
        isChampion |= isFinalBoss;
        isElite |= isChampion;
        if (!isElite || isChampion)
        {
            eliteAffix = EliteAffixKind.None;
        }

        var affixHealthMultiplier =
            EliteAffixUtility.ResolveHealthMultiplier(eliteAffix);
        var affixSpeedMultiplier =
            EliteAffixUtility.ResolveSpeedMultiplier(eliteAffix);
        var affixScaleMultiplier =
            EliteAffixUtility.ResolveScaleMultiplier(eliteAffix);
        var affixRewardMultiplier =
            EliteAffixUtility.ResolveRewardMultiplier(eliteAffix);
        var minAttackRange = math.max(0f, definition.Combat.MinAttackRange);
        var healthMultiplier =
            (1f + math.min(4f, elapsedSeconds / 150f)) *
            (isFinalBoss ? 25f : isChampion ? 10f : isElite ? 4f : 1f) *
            affixHealthMultiplier;
        var speedMultiplier =
            (1f + math.min(0.55f, elapsedSeconds / 900f)) *
            (isFinalBoss ? 0.72f : isChampion ? 0.8f : isElite ? 0.9f : 1f) *
            affixSpeedMultiplier;
        var rewardMultiplier =
            (isFinalBoss ? 40f : isChampion ? 15f : isElite ? 5f : 1f) *
            affixRewardMultiplier;
        var scaledHealth = (int)math.ceil(
            math.max(1, definition.Stats.MaxHealth) * healthMultiplier);

        EnsureTag<Enemy>(entityManager, actorEntity);
        if (isElite)
        {
            EnsureTag<EliteEnemy>(entityManager, actorEntity);
            if (eliteAffix != EliteAffixKind.None)
            {
                SetOrAddComponent(
                    entityManager,
                    actorEntity,
                    new EliteAffix { Kind = eliteAffix });
                if (eliteAffix == EliteAffixKind.Warded)
                {
                    SetOrAddComponent(
                        entityManager,
                        actorEntity,
                        EliteBarrierUtility.Create(scaledHealth));
                    SetOrAddComponent(
                        entityManager,
                        actorEntity,
                        new EliteBarrierBreakStagger());
                }
            }
        }
        if (isChampion)
        {
            EnsureTag<ChampionEnemy>(entityManager, actorEntity);
            if (!isFinalBoss)
            {
                SetOrAddComponent(
                    entityManager,
                    actorEntity,
                    new ChampionPhaseState());
            }
        }
        if (isFinalBoss)
        {
            EnsureTag<FinalBossEnemy>(entityManager, actorEntity);
            SetOrAddComponent(
                entityManager,
                actorEntity,
                new FinalBossPhaseState());
        }
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new EnemyTypeId { Value = definition.TypeId });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new Team { Value = TeamId.Enemy });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new EnemyDamageMultiplier
            {
                Value = EnemyDamageScaling.Resolve(
                    elapsedSeconds,
                    isChampion,
                    isFinalBoss,
                    eliteAffix),
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            Health.FromMax(scaledHealth));
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new Hitbox
            {
                Radius = definition.Stats.HitRadius *
                    (isFinalBoss ? 2.1f : isChampion ? 1.75f : isElite ? 1.35f : 1f) *
                    affixScaleMultiplier,
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new EnemyMoveSpeed
            {
                Value = math.max(
                    0.01f,
                    definition.Movement.MoveSpeed * speedMultiplier),
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new EnemyMovementPattern { Kind = definition.Movement.Kind });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new EnemyAttackRange
            {
                Min = minAttackRange,
                Max = math.max(
                    minAttackRange,
                    definition.Combat.MaxAttackRange),
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new AttackLoadout
            {
                PrimaryAttack = definition.Combat.PrimaryAttack,
            });
        SetOrAddComponent(entityManager, actorEntity, new AttackCooldown());
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new EnemyReward
            {
                Experience = math.max(
                    0,
                    (int)math.ceil(
                        definition.Reward.Experience * rewardMultiplier)),
                Score = math.max(
                    0,
                    (int)math.ceil(
                        definition.Reward.Score * rewardMultiplier)),
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new URPMaterialPropertyBaseColor
            {
                Value = isFinalBoss
                    ? new float4(0.95f, 0.04f, 0.08f, 1f)
                    : isChampion
                    ? new float4(0.72f, 0.08f, 1f, 1f)
                    : isElite
                    ? EliteAffixUtility.ResolveColor(eliteAffix)
                    : definition.Visual.Color,
            });

        var transform = entityManager.GetComponentData<LocalTransform>(actorEntity);
        transform.Scale = math.max(
            0.01f,
            definition.Stats.BodyScale *
            (isFinalBoss ? 2.7f : isChampion ? 2.1f : isElite ? 1.45f : 1f) *
            affixScaleMultiplier);
        entityManager.SetComponentData(actorEntity, transform);
    }

    private float3 GetEnemySpawnPosition(
        ref SystemState state,
        in SpawnMasterData spawnMaster)
    {
        var playerPosition = GetPlayerPosition(ref state);

        var angle = random.NextFloat(0f, 2f * math.PI);
        var distance = random.NextFloat(
            spawnMaster.MinSpawnDistance,
            spawnMaster.MaxSpawnDistance);
        return playerPosition +
            new float3(math.cos(angle), 0f, math.sin(angle)) * distance;
    }

    private float3 GetPlayerPosition(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            return SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        }

        return float3.zero;
    }

    private static void ApplyActorColorToChildren(
        EntityManager entityManager,
        Entity actorEntity)
    {
        if (!entityManager.HasComponent<ActorBody>(actorEntity) ||
            !entityManager.HasComponent<URPMaterialPropertyBaseColor>(actorEntity))
        {
            return;
        }

        var actorBody = entityManager.GetComponentData<ActorBody>(actorEntity);
        var color = entityManager.GetComponentData<URPMaterialPropertyBaseColor>(actorEntity);
        SetColorIfPresent(entityManager, actorBody.Turret, color);
        SetColorIfPresent(entityManager, actorBody.Canon, color);

        if (entityManager.HasBuffer<SyncColor>(actorEntity))
        {
            entityManager.RemoveComponent<SyncColor>(actorEntity);
        }
    }

    private static void SetColorIfPresent(
        EntityManager entityManager,
        Entity entity,
        URPMaterialPropertyBaseColor color)
    {
        if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
        {
            entityManager.SetComponentData(entity, color);
        }
    }

    private SpawnMasterData ResolveSpawnMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<SpawnMasterElement>(
                out var spawnMasters,
                true))
        {
            return SpawnMasterCatalog.Get(spawnMasters, SpawnMasterId.Default);
        }

        return SpawnMasterCatalog.Get(SystemAPI.GetSingleton<Config>());
    }

    private PlayerMasterData ResolvePlayerMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<PlayerMasterElement>(
                out var playerMasters,
                true))
        {
            return PlayerMasterCatalog.Get(playerMasters, PlayerMasterId.Default);
        }

        return PlayerMasterCatalog.Get(PlayerMasterId.Default);
    }

    private PlayerProgressMasterData ResolvePlayerProgressMaster(
        ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<PlayerProgressMasterElement>(
                out var progressMasters,
                true))
        {
            return PlayerProgressMasterCatalog.Get(
                progressMasters,
                PlayerProgressMasterId.Default);
        }

        return PlayerProgressMasterCatalog.Get(PlayerProgressMasterId.Default);
    }

    private static void EnsureTag<T>(
        EntityManager entityManager,
        Entity entity)
        where T : unmanaged, IComponentData
    {
        if (!entityManager.HasComponent<T>(entity))
        {
            entityManager.AddComponent<T>(entity);
        }
    }

    private static void SetOrAddComponent<T>(
        EntityManager entityManager,
        Entity entity,
        T value)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
        {
            entityManager.SetComponentData(entity, value);
        }
        else
        {
            entityManager.AddComponentData(entity, value);
        }
    }
}
