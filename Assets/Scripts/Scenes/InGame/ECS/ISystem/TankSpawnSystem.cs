using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial struct TankSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        
        var config = SystemAPI.GetSingleton<Config>();
        var random = new Random(123);

        for (int i = 0; i < config.TankCount; i++)
        {
            var tankEntity = state.EntityManager.Instantiate(config.TankPrefab);
            var color = new URPMaterialPropertyBaseColor { Value = RamdomColor(ref random) };

            // プレイヤー設定
            if (i == 0)
            {
                state.EntityManager.AddComponent<Player>(tankEntity);
                state.EntityManager.AddComponent<CameraTarget>(tankEntity);
            }
            
            // プレハブからインスタンス化されたすべてのルート エンティティには LinkedEntityGroup コンポーネントがあり、
            // は、プレハブ階層を構成するすべてのエンティティ (ルートを含む) のリストです。
            // (LinkedEntityGroup は「DynamicBuffer」と呼ばれる特別な種類のコンポーネントです。
            // 単一の構造体ではなく、サイズ変更可能な構造体の値の配列。)
            var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(tankEntity);
            foreach (var entity in linkedEntities)
            {
                if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                {
                    state.EntityManager.SetComponentData(entity.Value, color);
                }
            }
        }
    }

    // 視覚的に区別できるランダムな色を返します。
    // (単純なランダム性により、クラスター化された色の分布が生成されます 
    // 狭い範囲の色相の周り。 https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/ を参照してください)
    static float4 RamdomColor(ref Random random)
    { 
        // 0.618034005f は黄金比の逆数です
        var hue = (random.NextFloat() + 0.618034005f) % 1;  
        return (Vector4)Color.HSVToRGB(hue, 1, 1);
    }
}