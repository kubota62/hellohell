using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct ProjectileHitSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = true;
        
        state.RequireForUpdate<ProjectileMotion>();
        state.RequireForUpdate<Projectile>();
        state.RequireForUpdate<Tank>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. タンクのデータをJobに渡すために取得
        // QueryBuilderを使って、タンク全員の位置とEntityのリストを作成
        var tankQuery = SystemAPI.QueryBuilder().WithAll<Tank, LocalTransform, Team, Hitbox>().Build();
        
        // NativeArrayとして抽出（Allocator.TempJobでこのフレームのみ有効なメモリを確保）
        var tankEntities = tankQuery.ToEntityArray(state.WorldUpdateAllocator);
        var tankTransforms = tankQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
        var tankTeams = tankQuery.ToComponentDataArray<Team>(state.WorldUpdateAllocator);
        var tankHitboxes = tankQuery.ToComponentDataArray<Hitbox>(state.WorldUpdateAllocator);

        // 2. 並列書き込み用のECBを作成
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // 3. Jobのスケジュール
        var collisionJob = new ProjectileHitJob
        {
            ECB = ecb,
            TankEntities = tankEntities,
            TankTransforms = tankTransforms,
            TankTeams = tankTeams,
            TankHitboxes = tankHitboxes
        };

        // ScheduleParallelで全コアを使って実行
        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct ProjectileHitJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    
    [ReadOnly] public NativeArray<Entity> TankEntities;
    [ReadOnly] public NativeArray<LocalTransform> TankTransforms;
    [ReadOnly] public NativeArray<Team> TankTeams;
    [ReadOnly] public NativeArray<Hitbox> TankHitboxes;

    // このExecuteが「弾」の数だけ並列に呼ばれる
    // [EntityIndexInQuery] はParallelWriterの第一引数（sortKey）として必須
    [BurstCompile]
    private void Execute(
        [EntityIndexInQuery] int sortKey,
        Entity projectileEntity,
        in ProjectileMotion motion,
        in Projectile projectile,
        in LocalTransform bulletTransform)
    {
        // var hoge = "aaa";
        // var fuga = hoge.Substring(0, 1);
        // Debug.Log(hoge);
        // Debug.Log(fuga);
        float3 projectilePos = bulletTransform.Position;

        // 全てのタンクに対して距離をチェック
        for (int i = 0; i < TankEntities.Length; i++)
        {
            // 自分を撃ったタンクは無視
            if (TankEntities[i] == motion.Shooter) continue;
            if (!TeamUtility.AreHostile(projectile.Team, TankTeams[i].Value)) continue;

            float3 tankPos = TankTransforms[i].Position;
            var hitDistance = projectile.HitRadius + TankHitboxes[i].Radius;
            
            if (math.distancesq(projectilePos, tankPos) < hitDistance * hitDistance)
            {
                // 当たり判定成功時の処理をバッファに記録
                // sortKeyを渡すことで、並列処理でも実行順序が保証される
                ECB.AppendToBuffer(sortKey, TankEntities[i], new DamageEvent
                {
                    Damage = projectile.Damage,
                    Attacker = projectile.Owner,
                });
                
                // 弾を消す処理などもここに追加可能
                ECB.DestroyEntity(sortKey, projectileEntity);
            }
        }
    }
}
