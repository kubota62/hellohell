using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct BulletCollisionSystem_Job : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = true;
        
        state.RequireForUpdate<Bullet>();
        state.RequireForUpdate<Tank>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. タンクのデータをJobに渡すために取得
        // QueryBuilderを使って、タンク全員の位置とEntityのリストを作成
        var tankQuery = SystemAPI.QueryBuilder().WithAll<Tank, LocalTransform>().Build();
        
        // NativeArrayとして抽出（Allocator.TempJobでこのフレームのみ有効なメモリを確保）
        var tankEntities = tankQuery.ToEntityArray(state.WorldUpdateAllocator);
        var tankTransforms = tankQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

        // 2. 並列書き込み用のECBを作成
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // 3. Jobのスケジュール
        var collisionJob = new BulletCollisionJob
        {
            ECB = ecb,
            TankEntities = tankEntities,
            TankTransforms = tankTransforms
        };

        // ScheduleParallelで全コアを使って実行
        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct BulletCollisionJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    
    [ReadOnly] public NativeArray<Entity> TankEntities;
    [ReadOnly] public NativeArray<LocalTransform> TankTransforms;

    // このExecuteが「弾」の数だけ並列に呼ばれる
    // [EntityIndexInQuery] はParallelWriterの第一引数（sortKey）として必須
    [BurstCompile]
    private void Execute([EntityIndexInQuery] int sortKey, Entity bulletEntity, in Bullet bullet, in LocalTransform bulletTransform)
    {
        // var hoge = "aaa";
        // var fuga = hoge.Substring(0, 1);
        // Debug.Log(hoge);
        // Debug.Log(fuga);
        float3 bulletPos = bulletTransform.Position;

        // 全てのタンクに対して距離をチェック
        for (int i = 0; i < TankEntities.Length; i++)
        {
            // 自分を撃ったタンクは無視
            if (TankEntities[i] == bullet.Shooter) continue;

            float3 tankPos = TankTransforms[i].Position;
            
            if (math.distance(bulletPos, tankPos) < 2f)
            {
                // 当たり判定成功時の処理をバッファに記録
                // sortKeyを渡すことで、並列処理でも実行順序が保証される
                ECB.AppendToBuffer(sortKey, TankEntities[i], new DamageEvent
                {
                    Damage = 34,
                    Attacker = bulletEntity,
                });
                
                // 弾を消す処理などもここに追加可能
                ECB.DestroyEntity(sortKey, bulletEntity);
            }
        }
    }
}