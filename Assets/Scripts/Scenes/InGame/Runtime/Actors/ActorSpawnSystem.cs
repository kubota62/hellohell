using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 初期 Player と継続的な Enemy を ActorBody プレハブから生成するシステム。
/// 生成直後の位置を同フレームの描画へ反映するため、TransformSystemGroup より前に実行する。
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
            var enemyTypeId = EnemyDefinitionCatalog.PickSpawnType(spawnedCount);
            var definition = hasEnemyDefinitions
                ? EnemyDefinitionCatalog.Get(enemyDefinitions, enemyTypeId)
                : EnemyDefinitionCatalog.Get(enemyTypeId);
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
        entityManager.SetComponentData(actorEntity, new Team { Value = TeamId.Enemy });
        entityManager.SetComponentData(actorEntity, Health.FromMax(definition.MaxHealth));
        entityManager.SetComponentData(actorEntity, new Hitbox { Radius = definition.HitRadius });
        entityManager.SetComponentData(actorEntity, new AttackLoadout
        {
            PrimaryAttack = definition.PrimaryAttack,
        });
        entityManager.SetComponentData(actorEntity, new AttackCooldown());

        switch (definition.Movement)
        {
            case EnemyMovementKind.Random:
                entityManager.AddComponent<EnemyMovementRandom>(actorEntity);
                break;

            case EnemyMovementKind.Forward:
            default:
                entityManager.AddComponent<EnemyMovementForward>(actorEntity);
                break;
        }

        SetOrAddColor(entityManager, actorEntity, new URPMaterialPropertyBaseColor
        {
            Value = definition.Color,
        });
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
