using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial struct TankSpawnSystem : ISystem
{
    private Random Rand;
    private int spawndCount;
    private float spawnTimer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        Rand = new Random(123);
        spawndCount = 0;
        spawnTimer = 0;

        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 最初のフレームでプレイヤーを生成する
        if (spawndCount == 0)
        {
            SpawnTank(true, float3.zero, ref state);
            spawndCount++;
            return;
        }

        // 以降は一定時間ごとにプレイヤーから離れた位置に敵を生成する
        var config = SystemAPI.GetSingleton<Config>();
        spawnTimer += SystemAPI.Time.DeltaTime;
        if (spawnTimer > config.SpawnTime)
        {
            SpawnTank(false, GetEnemySpawnPosition(config, ref state), ref state);

            spawnTimer = 0;
            spawndCount++;
        }
    }

    // プレイヤーから一定距離離れたランダムな座標を返す
    [BurstCompile]
    private float3 GetEnemySpawnPosition(in Config config, ref SystemState state)
    {
        var playerPosition = float3.zero;
        if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        }

        // ランダムな方向 × ランダムな距離 のオフセットを作る (XZ平面)
        var angle = Rand.NextFloat(0f, 2f * math.PI);
        var distance = Rand.NextFloat(config.MinSpawnDistance, config.MaxSpawnDistance);
        var offset = new float3(math.cos(angle), 0f, math.sin(angle)) * distance;

        return playerPosition + offset;
    }

    [BurstCompile]
    private void SpawnTank(bool isPlayer, float3 position, ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var tankEntity = ecb.Instantiate(config.TankPrefab);
        ecb.SetComponent(tankEntity, LocalTransform.FromPosition(position));

        // プレイヤー設定
        if (isPlayer)
        {
            ecb.AddComponent<Player>(tankEntity);
            ecb.AddComponent<CameraTarget>(tankEntity);
        }

        var color = new URPMaterialPropertyBaseColor { Value = RamdomColor(ref Rand) };
        ecb.AddComponent(tankEntity, color);
    }

    // 視覚的に区別できるランダムな色を返します。
    // (単純なランダム性により、クラスター化された色の分布が生成されます
    // 狭い範囲の色相の周り。 https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/ を参照してください)
    static float4 RamdomColor(ref Random Rand)
    {
        // 0.618034005f は黄金比の逆数です
        var hue = (Rand.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1, 1);
    }
}
