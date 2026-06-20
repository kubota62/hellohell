using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 起動時一回だけ色を親に合わせて変更する
/// </summary>
[BurstCompile]
public partial struct SyncColorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>(); // Configがあるまで実行しない
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var job = new SyncColorWithParentJob
        {
            ColorLookup = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(true),
            ECB = ecb
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct SyncColorWithParentJob : IJobEntity
{ 
    [ReadOnly] public ComponentLookup<URPMaterialPropertyBaseColor> ColorLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    // Executeの引数にEntity自身を追加
    private void Execute(
        [EntityIndexInQuery]
        int sortKey,
        Entity entity,
        in URPMaterialPropertyBaseColor myColor,
        in DynamicBuffer<SyncColor> targets)
    {
        // 1. リスト（Buffer）内の全Entityに対してループ
        for (int i = 0; i < targets.Length; i++)
        {
            Entity targetEntity = targets[i].SyncTarget;

            // ターゲットが指定のコンポーネントを持っているか確認して書き換え
            if (ColorLookup.HasComponent(targetEntity))
            {
                // 直接書き換えず、ECBに予約を入れる
                ECB.SetComponent(sortKey, targetEntity, new URPMaterialPropertyBaseColor { Value = myColor.Value });
            }
        }

        // 2. 実行後にこのBuffer(またはコンポーネント)を削除して、1回きりの実行にする
        // ※これをしないと毎フレーム色を上書きし続けます
        ECB.RemoveComponent<SyncColor>(sortKey, entity);
    }
}