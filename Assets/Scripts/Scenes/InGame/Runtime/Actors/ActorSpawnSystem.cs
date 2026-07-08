using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 初期Playerと継続的なEnemyをActorBodyプレハブから生成するシステム。
/// 生成直後の位置を同フレームの描画へ反映するため、TransformSystemGroupより前に実行する。
/// </summary>
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ActorSpawnSystem : ISystem
{
    private Random Rand;
    private int spawnedCount;
    private float spawnTimer;

    public void OnCreate(ref SystemState state)
    {
        Rand = new Random(123);
        spawnedCount = 0;
        spawnTimer = 0;

        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (spawnedCount == 0)
        {
            SpawnActor(true, float3.zero, ref state);
            spawnedCount++;
            spawnTimer = -ResolveSpawnMaster(ref state).InitialEnemySpawnDelay;
            return;
        }

        var spawnMaster = ResolveSpawnMaster(ref state);
        spawnTimer += SystemAPI.Time.DeltaTime;
        if (spawnTimer > spawnMaster.SpawnInterval)
        {
            SpawnActor(false, GetEnemySpawnPosition(spawnMaster, ref state), ref state);

            spawnTimer = 0;
            spawnedCount++;
        }
    }

    private SpawnMasterData ResolveSpawnMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<SpawnMasterElement>(out var spawnMasters, true))
        {
            return SpawnMasterCatalog.Get(spawnMasters, SpawnMasterId.Default);
        }

        var config = SystemAPI.GetSingleton<Config>();
        return SpawnMasterCatalog.Get(config);
    }

    private PlayerProgressMasterData ResolvePlayerProgressMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<PlayerProgressMasterElement>(out var progressMasters, true))
        {
            return PlayerProgressMasterCatalog.Get(progressMasters, PlayerProgressMasterId.Default);
        }

        return PlayerProgressMasterCatalog.Get(PlayerProgressMasterId.Default);
    }

    private PlayerMasterData ResolvePlayerMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<PlayerMasterElement>(out var playerMasters, true))
        {
            return PlayerMasterCatalog.Get(playerMasters, PlayerMasterId.Default);
        }

        return PlayerMasterCatalog.Get(PlayerMasterId.Default);
    }

    private float3 GetEnemySpawnPosition(in SpawnMasterData spawnMaster, ref SystemState state)
    {
        var playerPosition = float3.zero;
        if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        }

        var angle = Rand.NextFloat(0f, 2f * math.PI);
        var distance = Rand.NextFloat(spawnMaster.MinSpawnDistance, spawnMaster.MaxSpawnDistance);
        var offset = new float3(math.cos(angle), 0f, math.sin(angle)) * distance;

        return playerPosition + offset;
    }

    private void SpawnActor(bool isPlayer, float3 position, ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var entityManager = state.EntityManager;

        var actorEntity = entityManager.Instantiate(config.ActorPrefab);
        var rotation = Quaternion.Euler(0f, Rand.NextFloat(0f, 360f), 0f);
        entityManager.SetComponentData(actorEntity, LocalTransform.FromPositionRotation(position, rotation));
        ResetMoveIntent(entityManager, actorEntity);

        if (isPlayer)
        {
            AddPlayerComponents(
                entityManager,
                actorEntity,
                ResolvePlayerMaster(ref state),
                ResolvePlayerProgressMaster(ref state));
        }
        else
        {
            var hasEnemyMasters = SystemAPI.TryGetSingletonBuffer<EnemyMasterElement>(
                out var enemyMasters,
                true);
            var enemySpawnIndex = spawnedCount - 1;
            var playerLevel = ResolvePlayerLevel(ref state);
            var definition = hasEnemyMasters
                ? EnemyMasterCatalog.PickSpawnMaster(enemyMasters, enemySpawnIndex, playerLevel)
                : EnemyMasterCatalog.Get(EnemyMasterCatalog.PickSpawnType(enemySpawnIndex));
            AddEnemyComponents(entityManager, actorEntity, definition);
        }

        ApplySpawnedActorColor(ref state, actorEntity);
    }

    private int ResolvePlayerLevel(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<PlayerProgress>(out var progress))
        {
            return progress.Level;
        }

        return 1;
    }

    private static void AddPlayerComponents(
        EntityManager entityManager,
        Entity actorEntity,
        PlayerMasterData playerMaster,
        PlayerProgressMasterData progressMaster)
    {
        entityManager.AddComponent<Player>(actorEntity);
        entityManager.AddComponent<CameraTarget>(actorEntity);
        entityManager.SetComponentData(actorEntity, new Team { Value = TeamId.Player });
        entityManager.SetComponentData(actorEntity, new AttackLoadout
        {
            PrimaryAttack = playerMaster.PrimaryAttack,
        });
        entityManager.SetComponentData(actorEntity, new AttackCooldown());
        entityManager.AddComponentData(actorEntity, PlayerProgressSystem.CreateInitialProgress(progressMaster));
        entityManager.AddComponentData(actorEntity, new PlayerSkillStats());

        var playerColor = new URPMaterialPropertyBaseColor { Value = new float4(1f, 1f, 1f, 1f) };
        SetOrAddColor(entityManager, actorEntity, playerColor);
    }

    private static void AddEnemyComponents(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyMasterData definition)
    {
        entityManager.AddComponent<Enemy>(actorEntity);
        entityManager.AddComponentData(actorEntity, new EnemyTypeId { Value = definition.TypeId });

        ApplyEnemyStats(entityManager, actorEntity, definition.Stats);
        ApplyEnemyMovement(entityManager, actorEntity, definition.Movement);
        ApplyEnemyCombat(entityManager, actorEntity, definition.Combat);
        ApplyEnemyVisual(entityManager, actorEntity, definition.Visual);
        ApplyEnemyReward(entityManager, actorEntity, definition.Reward);
    }

    /// <summary>
    /// HP、当たり判定、見た目サイズなど、敵の身体に関わる値をまとめて適用する。
    /// </summary>
    private static void ApplyEnemyStats(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyStatMaster stats)
    {
        entityManager.SetComponentData(actorEntity, new Team { Value = TeamId.Enemy });
        entityManager.SetComponentData(actorEntity, Health.FromMax(stats.MaxHealth));
        entityManager.SetComponentData(actorEntity, new Hitbox { Radius = stats.HitRadius });

        var transform = entityManager.GetComponentData<LocalTransform>(actorEntity);
        transform.Scale = math.max(0.01f, stats.BodyScale);
        entityManager.SetComponentData(actorEntity, transform);
    }

    /// <summary>
    /// 敵定義で選ばれた移動タイプに応じて、必要な移動コンポーネントを付与する。
    /// </summary>
    private static void ApplyEnemyMovement(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyMovementMaster movement)
    {
        SetOrAddMoveSpeed(entityManager, actorEntity, movement.MoveSpeed);

        switch (movement.Kind)
        {
            case EnemyMovementKind.Kite:
                entityManager.AddComponent<EnemyMovementKite>(actorEntity);
                break;

            case EnemyMovementKind.Random:
                entityManager.AddComponent<EnemyMovementRandom>(actorEntity);
                break;

            case EnemyMovementKind.Forward:
            default:
                entityManager.AddComponent<EnemyMovementForward>(actorEntity);
                break;
        }
    }

    /// <summary>
    /// 敵定義で選ばれた攻撃IDと攻撃距離を、攻撃システムが読める形に変換する。
    /// </summary>
    private static void ApplyEnemyCombat(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyCombatMaster combat)
    {
        SetOrAddAttackRange(entityManager, actorEntity, combat.MinAttackRange, combat.MaxAttackRange);
        entityManager.SetComponentData(actorEntity, new AttackLoadout
        {
            PrimaryAttack = combat.PrimaryAttack,
        });
        entityManager.SetComponentData(actorEntity, new AttackCooldown());
    }

    /// <summary>
    /// 本体と子パーツへ同期するため、敵定義の色をActor本体に付与する。
    /// </summary>
    private static void ApplyEnemyVisual(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyVisualMaster visual)
    {
        SetOrAddColor(entityManager, actorEntity, new URPMaterialPropertyBaseColor
        {
            Value = visual.Color,
        });
    }

    /// <summary>
    /// 死亡時に報酬イベントへ変換できるよう、敵ごとの報酬値をActorへ持たせる。
    /// </summary>
    private static void ApplyEnemyReward(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyRewardMaster reward)
    {
        entityManager.AddComponentData(actorEntity, new EnemyReward
        {
            Experience = math.max(0, reward.Experience),
            Score = math.max(0, reward.Score),
        });
    }

    private static void SetOrAddMoveSpeed(
        EntityManager entityManager,
        Entity actorEntity,
        float moveSpeed)
    {
        var speed = new EnemyMoveSpeed { Value = math.max(0.01f, moveSpeed) };
        if (entityManager.HasComponent<EnemyMoveSpeed>(actorEntity))
        {
            entityManager.SetComponentData(actorEntity, speed);
        }
        else
        {
            entityManager.AddComponentData(actorEntity, speed);
        }
    }

    private static void SetOrAddAttackRange(
        EntityManager entityManager,
        Entity actorEntity,
        float min,
        float max)
    {
        var minRange = math.max(0f, min);
        var range = new EnemyAttackRange
        {
            Min = minRange,
            Max = math.max(minRange, max),
        };

        if (entityManager.HasComponent<EnemyAttackRange>(actorEntity))
        {
            entityManager.SetComponentData(actorEntity, range);
        }
        else
        {
            entityManager.AddComponentData(actorEntity, range);
        }
    }

    private static void ApplySpawnedActorColor(ref SystemState state, Entity actorEntity)
    {
        if (!state.EntityManager.HasComponent<ActorBody>(actorEntity) ||
            !state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(actorEntity))
        {
            return;
        }

        var actorBody = state.EntityManager.GetComponentData<ActorBody>(actorEntity);
        var color = state.EntityManager.GetComponentData<URPMaterialPropertyBaseColor>(actorEntity);

        if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(actorBody.Turret))
        {
            state.EntityManager.SetComponentData(actorBody.Turret, color);
        }

        if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(actorBody.Canon))
        {
            state.EntityManager.SetComponentData(actorBody.Canon, color);
        }

        if (state.EntityManager.HasBuffer<SyncColor>(actorEntity))
        {
            state.EntityManager.RemoveComponent<SyncColor>(actorEntity);
        }
    }

    private static void SetOrAddColor(
        EntityManager entityManager,
        Entity entity,
        URPMaterialPropertyBaseColor color)
    {
        if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
        {
            entityManager.SetComponentData(entity, color);
        }
        else
        {
            entityManager.AddComponentData(entity, color);
        }
    }

    private static void ResetMoveIntent(EntityManager entityManager, Entity actorEntity)
    {
        var intent = new MoveIntent
        {
            Direction = float3.zero,
            Magnitude = 0f,
        };

        if (entityManager.HasComponent<MoveIntent>(actorEntity))
        {
            entityManager.SetComponentData(actorEntity, intent);
        }
        else
        {
            entityManager.AddComponentData(actorEntity, intent);
        }
    }
}
