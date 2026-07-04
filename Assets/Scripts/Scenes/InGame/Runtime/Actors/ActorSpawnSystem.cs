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
    const float InitialEnemySpawnDelay = 1f;

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
            spawnTimer = -InitialEnemySpawnDelay;
            return;
        }

        var config = SystemAPI.GetSingleton<Config>();
        spawnTimer += SystemAPI.Time.DeltaTime;
        if (spawnTimer > config.SpawnTime)
        {
            SpawnActor(false, GetEnemySpawnPosition(config, ref state), ref state);

            spawnTimer = 0;
            spawnedCount++;
        }
    }

    private float3 GetEnemySpawnPosition(in Config config, ref SystemState state)
    {
        var playerPosition = float3.zero;
        if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        }

        var angle = Rand.NextFloat(0f, 2f * math.PI);
        var distance = Rand.NextFloat(config.MinSpawnDistance, config.MaxSpawnDistance);
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
            AddPlayerComponents(entityManager, actorEntity);
        }
        else
        {
            var hasEnemyDefinitions = SystemAPI.TryGetSingletonBuffer<EnemyDefinitionElement>(
                out var enemyDefinitions,
                true);
            var enemySpawnIndex = spawnedCount - 1;
            var definition = hasEnemyDefinitions
                ? EnemyDefinitionCatalog.PickSpawnDefinition(enemyDefinitions, enemySpawnIndex)
                : EnemyDefinitionCatalog.Get(EnemyDefinitionCatalog.PickSpawnType(enemySpawnIndex));
            AddEnemyComponents(entityManager, actorEntity, definition);
        }

        ApplySpawnedActorColor(ref state, actorEntity);
    }

    private static void AddPlayerComponents(EntityManager entityManager, Entity actorEntity)
    {
        entityManager.AddComponent<Player>(actorEntity);
        entityManager.AddComponent<CameraTarget>(actorEntity);
        entityManager.SetComponentData(actorEntity, new Team { Value = TeamId.Player });
        entityManager.SetComponentData(actorEntity, new AttackLoadout
        {
            PrimaryAttack = AttackDefinitionId.BasicMeleeArc,
        });
        entityManager.SetComponentData(actorEntity, new AttackCooldown());

        var playerColor = new URPMaterialPropertyBaseColor { Value = new float4(1f, 1f, 1f, 1f) };
        SetOrAddColor(entityManager, actorEntity, playerColor);
    }

    private static void AddEnemyComponents(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyDefinitionData definition)
    {
        entityManager.AddComponent<Enemy>(actorEntity);
        entityManager.AddComponentData(actorEntity, new EnemyTypeId { Value = definition.TypeId });

        ApplyEnemyStats(entityManager, actorEntity, definition.Stats);
        ApplyEnemyMovement(entityManager, actorEntity, definition.Movement);
        ApplyEnemyCombat(entityManager, actorEntity, definition.Combat);
        ApplyEnemyVisual(entityManager, actorEntity, definition.Visual);
    }

    private static void ApplyEnemyStats(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyStatDefinition stats)
    {
        entityManager.SetComponentData(actorEntity, new Team { Value = TeamId.Enemy });
        entityManager.SetComponentData(actorEntity, Health.FromMax(stats.MaxHealth));
        entityManager.SetComponentData(actorEntity, new Hitbox { Radius = stats.HitRadius });

        var transform = entityManager.GetComponentData<LocalTransform>(actorEntity);
        transform.Scale = math.max(0.01f, stats.BodyScale);
        entityManager.SetComponentData(actorEntity, transform);
    }

    private static void ApplyEnemyMovement(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyMovementDefinition movement)
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

    private static void ApplyEnemyCombat(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyCombatDefinition combat)
    {
        SetOrAddAttackRange(entityManager, actorEntity, combat.MinAttackRange, combat.MaxAttackRange);
        entityManager.SetComponentData(actorEntity, new AttackLoadout
        {
            PrimaryAttack = combat.PrimaryAttack,
        });
        entityManager.SetComponentData(actorEntity, new AttackCooldown());
    }

    private static void ApplyEnemyVisual(
        EntityManager entityManager,
        Entity actorEntity,
        EnemyVisualDefinition visual)
    {
        SetOrAddColor(entityManager, actorEntity, new URPMaterialPropertyBaseColor
        {
            Value = visual.Color,
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
