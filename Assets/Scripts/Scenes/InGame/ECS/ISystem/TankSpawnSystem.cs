using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial struct TankSpawnSystem : ISystem
{
    private static readonly float SpawnTime = 0.5f;

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
        spawnTimer += Time.deltaTime;
        if (spawnTimer > SpawnTime)
        {
            SpawnTank(spawndCount == 0, ref state);

            spawnTimer = 0;
            spawndCount++;
        }
    }

    [BurstCompile]
    private void SpawnTank(bool isPlayer, ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var tankEntity = ecb.Instantiate(config.TankPrefab);

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