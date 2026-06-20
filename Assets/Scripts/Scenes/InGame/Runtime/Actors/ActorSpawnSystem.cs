using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 初期 Player と継続的な Enemy を ActorBody プレハブから生成するシステム。
/// 将来は EnemyDefinition から EnemyTypeId、移動タグ、武器構成を選ぶ入口になる。
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
        // 最初のフレームでプレイヤーを生成する。
        if (spawnedCount == 0)
        {
            SpawnActor(true, float3.zero, ref state);
            spawnedCount++;
            return;
        }

        // 以降は一定時間ごとに、プレイヤーから離れた位置へ敵を生成する。
        var config = SystemAPI.GetSingleton<Config>();
        spawnTimer += SystemAPI.Time.DeltaTime;
        if (spawnTimer > config.SpawnTime)
        {
            SpawnActor(false, GetEnemySpawnPosition(config, ref state), ref state);

            spawnTimer = 0;
            spawnedCount++;
        }
    }

    // プレイヤーから一定距離離れたランダムな座標を返す。
    [BurstCompile]
    private float3 GetEnemySpawnPosition(in Config config, ref SystemState state)
    {
        var playerPosition = float3.zero;
        if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        }

        // ランダムな方向と距離から XZ 平面上のオフセットを作る。
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
        Quaternion rot = Quaternion.Euler(0f, Rand.NextInt() % 360f, 0f);
        ecb.SetComponent(actorEntity, LocalTransform.FromPositionRotation(position, rot));

        if (isPlayer)
        {
            // プレイヤー用のタグと所属を付与する。
            ecb.AddComponent<Player>(actorEntity);
            ecb.AddComponent<CameraTarget>(actorEntity);
            ecb.SetComponent(actorEntity, new Team { Value = TeamId.Player });

            var playerColor = new URPMaterialPropertyBaseColor { Value = new(255, 255, 255, 255) };
            ecb.AddComponent(actorEntity, playerColor);
        }
        else
        {
            // 敵用のタグ、種類ID、所属を付与する。
            ecb.AddComponent<Enemy>(actorEntity);
            ecb.AddComponent(actorEntity, new EnemyTypeId { Value = 1 });
            ecb.SetComponent(actorEntity, new Team { Value = TeamId.Enemy });
            if (spawnedCount % 2 == 0)
            {
                ecb.AddComponent<EnemyMovementRandom>(actorEntity);
                var color1 = new URPMaterialPropertyBaseColor { Value = new(1f, 1f, 0, 0) };
                ecb.AddComponent(actorEntity, color1);
            }
            else
            {
                ecb.AddComponent<EnemyMovementForward>(actorEntity);
                var color2 = new URPMaterialPropertyBaseColor { Value = new(1f, 0, 1f, 0) };
                ecb.AddComponent(actorEntity, color2);
            }

            // var color = new URPMaterialPropertyBaseColor { Value = RandomColor(ref Rand) };
            // ecb.AddComponent(actorEntity, color);
        }
    }

    // 視覚的に区別しやすいランダム色を返す。
    // 黄金比を使って色相が偏りすぎないようにする。
    static float4 RandomColor(ref Random Rand)
    {
        // 0.618034005f は黄金比の逆数。
        var hue = (Rand.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1, 1);
    }
}
