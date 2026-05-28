// using Unity.Burst;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// /// <summary>
// /// シンプルな距離チェックによるBulletとTankの当たり判定
// /// 軽量だが、エンティティ数が多い場合は非効率
// /// </summary>
// [BurstCompile]
// public partial struct BulletTankCollisionSystem_Simple : ISystem
// {
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//     {
//         // このSystemはデフォルトで無効
//         state.Enabled = false;
//     }
//     
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//         var collisionJob = new BulletTankCollisionJob_Simple
//         {
//             ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged),
//             CollisionRadius = 1.5f // Bullet半径 + Tank半径
//         };
//         
//         collisionJob.Schedule();
//     }
// }
//
// [BurstCompile]
// public partial struct BulletTankCollisionJob_Simple : IJobEntity
// {
//     public EntityCommandBuffer ECB;
//     public float CollisionRadius;
//
//     void Execute(Entity bulletEntity, in Bullet bullet, in LocalTransform bulletTransform)
//     {
//         // 全Tankとの距離をチェック
//         foreach (var (tankTransform, tankEntity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Tank>().WithEntityAccess())
//         {
//             float distanceSq = math.distancesq(bulletTransform.Position, tankTransform.ValueRO.Position);
//             float radiusSq = CollisionRadius * CollisionRadius;
//             
//             if (distanceSq < radiusSq)
//             {
//                 // 衝突検出 - Bulletを削除
//                 ECB.DestroyEntity(bulletEntity);
//                 
//                 // TODO: Tankにダメージを与える処理などをここに追加
//                 // ECB.SetComponentEnabled<TankHealth>(tankEntity, false); など
//                 
//                 break; // 1つのBulletは1つのTankにしか当たらない
//             }
//         }
//     }
// }
