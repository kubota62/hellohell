using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 初期 Player と継続的な Enemy を ActorBody プレハブから生成するシステム。
/// Enemy は定義バッファまたは EnemyDefinitionCatalog から種類、移動タグ、武器構成、見た目を選ぶ。
/// </summary>
public partial struct ActorSpawnSystem : ISystem
{
    private Random Rand;
    private int spawnedCount;
    private float spawnTimer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        Rand = new Random(123);
        spawnedCount = 0;
        spawnTimer = 0;

        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (spawnedCount == 0)
        {
            SpawnActor(true, float3.zero, ref state);
            spawnedCount++;
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

    [BurstCompile]
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

    [BurstCompile]
    private void SpawnActor(bool isPlayer, float3 position, ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var actorEntity = ecb.Instantiate(config.ActorPrefab);
        Quaternion rot = Quaternion.Euler(0f, Rand.NextFloat(0f, 360f), 0f);
        ecb.SetComponent(actorEntity, LocalTransform.FromPositionRotation(position, rot));

        if (isPlayer)
        {
            AddPlayerComponents(ecb, actorEntity);
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
            AddEnemyComponents(ecb, actorEntity, definition);
        }
    }

    private void AddPlayerComponents(EntityCommandBuffer ecb, Entity actorEntity)
    {
        ecb.AddComponent<Player>(actorEntity);
        ecb.AddComponent<CameraTarget>(actorEntity);
        ecb.SetComponent(actorEntity, new Team { Value = TeamId.Player });
        ecb.SetComponent(actorEntity, new AttackLoadout
        {
            PrimaryAttack = AttackDefinitionId.BasicProjectile,
        });

        var playerColor = new URPMaterialPropertyBaseColor { Value = new float4(1f, 1f, 1f, 1f) };
        ecb.AddComponent(actorEntity, playerColor);
    }

    private void AddEnemyComponents(
        EntityCommandBuffer ecb,
        Entity actorEntity,
        EnemyDefinitionData definition)
    {
        ecb.AddComponent<Enemy>(actorEntity);
        ecb.AddComponent(actorEntity, new EnemyTypeId { Value = definition.TypeId });
        ecb.SetComponent(actorEntity, new Team { Value = TeamId.Enemy });
        ecb.SetComponent(actorEntity, Health.FromMax(definition.MaxHealth));
        ecb.SetComponent(actorEntity, new Hitbox { Radius = definition.HitRadius });
        ecb.SetComponent(actorEntity, new AttackLoadout
        {
            PrimaryAttack = definition.PrimaryAttack,
        });

        switch (definition.Movement)
        {
            case EnemyMovementKind.Random:
                ecb.AddComponent<EnemyMovementRandom>(actorEntity);
                break;

            case EnemyMovementKind.Forward:
            default:
                ecb.AddComponent<EnemyMovementForward>(actorEntity);
                break;
        }

        ecb.AddComponent(actorEntity, new URPMaterialPropertyBaseColor
        {
            Value = definition.Color,
        });
    }

    static float4 RandomColor(ref Random Rand)
    {
        var hue = (Rand.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1, 1);
    }
}
