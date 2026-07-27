using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 共通Actor PrefabからPlayerとEnemyを生成し、マスター設定を適用する。
/// </summary>
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ActorSpawnSystem : ISystem
{
    private const int MaximumActiveEnemies = 350;
    private const float ChampionSpawnInterval = 90f;
    private const float HordeSurgeInterval = 60f;
    private const float RunDurationSeconds = 600f;

    private Random random;
    private int spawnedActorCount;
    private int championWavesSpawned;
    private int hordeWavesSpawned;
    private float enemySpawnTimer;
    private EntityQuery activeEnemyQuery;

    public void OnCreate(ref SystemState state)
    {
        random = new Random(123);
        spawnedActorCount = 0;
        championWavesSpawned = 0;
        hordeWavesSpawned = 0;
        enemySpawnTimer = 0f;
        activeEnemyQuery = SystemAPI.QueryBuilder()
            .WithAll<Enemy, GameplayActive>()
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

        if (runState.ElapsedSeconds >= runState.DurationSeconds)
        {
            runState.ElapsedSeconds = runState.DurationSeconds;
            runState.IsVictory = 1;
            runState.IsGameOver = 1;
            SystemAPI.SetSingleton(runState);
            return;
        }

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
        var championWave = (int)math.floor(
            runState.ElapsedSeconds / ChampionSpawnInterval);
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
            runState.ElapsedSeconds / HordeSurgeInterval);
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
        bool isChampion = false)
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
            ConfigureEnemy(
                entityManager,
                actorEntity,
                ResolveEnemyMaster(ref state, threatLevel),
                elapsedSeconds,
                isElite,
                isChampion);
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
        bool isChampion)
    {
        isElite |= isChampion;
        var minAttackRange = math.max(0f, definition.Combat.MinAttackRange);
        var healthMultiplier =
            (1f + math.min(4f, elapsedSeconds / 150f)) *
            (isChampion ? 10f : isElite ? 4f : 1f);
        var speedMultiplier =
            (1f + math.min(0.55f, elapsedSeconds / 900f)) *
            (isChampion ? 0.8f : isElite ? 0.9f : 1f);
        var rewardMultiplier = isChampion ? 15 : isElite ? 5 : 1;
        var scaledHealth = (int)math.ceil(
            math.max(1, definition.Stats.MaxHealth) * healthMultiplier);

        EnsureTag<Enemy>(entityManager, actorEntity);
        if (isElite)
        {
            EnsureTag<EliteEnemy>(entityManager, actorEntity);
        }
        if (isChampion)
        {
            EnsureTag<ChampionEnemy>(entityManager, actorEntity);
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
            Health.FromMax(scaledHealth));
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new Hitbox
            {
                Radius = definition.Stats.HitRadius *
                    (isChampion ? 1.75f : isElite ? 1.35f : 1f),
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
                    definition.Reward.Experience * rewardMultiplier),
                Score = math.max(
                    0,
                    definition.Reward.Score * rewardMultiplier),
            });
        SetOrAddComponent(
            entityManager,
            actorEntity,
            new URPMaterialPropertyBaseColor
            {
                Value = isChampion
                    ? new float4(0.72f, 0.08f, 1f, 1f)
                    : isElite
                    ? new float4(1f, 0.22f, 0.04f, 1f)
                    : definition.Visual.Color,
            });

        var transform = entityManager.GetComponentData<LocalTransform>(actorEntity);
        transform.Scale = math.max(
            0.01f,
            definition.Stats.BodyScale *
            (isChampion ? 2.1f : isElite ? 1.45f : 1f));
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
