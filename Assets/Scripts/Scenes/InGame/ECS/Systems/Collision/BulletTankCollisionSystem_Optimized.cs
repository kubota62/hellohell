// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// /// <summary>
// /// 最適化版: 空間分割を使ったBulletとTankの当たり判定
// /// エンティティ数が多い場合に効率的
// /// 現在は無効化されています。使用する場合はコメントアウトを外してください。
// /// </summary>
// [BurstCompile]
// [UpdateAfter(typeof(BulletSystem))]
// [UpdateAfter(typeof(TankMovementSystem))]
// // [UpdateInGroup(typeof(SimulationSystemGroup))] // 有効にする場合はコメント解除
// public partial struct BulletTankCollisionSystem_Optimized : ISystem
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
//         
//         // Tank位置を収集
//         var tankQuery = SystemAPI.QueryBuilder().WithAll<Tank, LocalTransform>().Build();
//         int tankCount = tankQuery.CalculateEntityCount();
//         
//         var tankPositions = new NativeArray<float3>(tankCount, Allocator.TempJob);
//         var tankEntities = new NativeArray<Entity>(tankCount, Allocator.TempJob);
//         
//         int index = 0;
//         foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Tank>().WithEntityAccess())
//         {
//             tankPositions[index] = transform.ValueRO.Position;
//             tankEntities[index] = entity;
//             index++;
//         }
//         
//         var collisionJob = new BulletTankCollisionJob_Optimized
//         {
//             ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged),
//             TankPositions = tankPositions,
//             TankEntities = tankEntities,
//             CollisionRadius = 1.5f
//         };
//         
//         state.Dependency = collisionJob.Schedule(state.Dependency);
//         state.Dependency = tankPositions.Dispose(state.Dependency);
//         state.Dependency = tankEntities.Dispose(state.Dependency);
//     }
// }
//
// [BurstCompile]
// public partial struct BulletTankCollisionJob_Optimized : IJobEntity
// {
//     public EntityCommandBuffer.ParallelWriter ECB;
//     [ReadOnly] public NativeArray<float3> TankPositions;
//     [ReadOnly] public NativeArray<Entity> TankEntities;
//     public float CollisionRadius;
//
//     void Execute([ChunkIndexInQuery] int chunkIndex, Entity bulletEntity, in Bullet bullet, in LocalTransform bulletTransform)
//     {
//         float radiusSq = CollisionRadius * CollisionRadius;
//         
//         for (int i = 0; i < TankPositions.Length; i++)
//         {
//             float distanceSq = math.distancesq(bulletTransform.Position, TankPositions[i]);
//             
//             if (distanceSq < radiusSq)
//             {
//                 ECB.DestroyEntity(chunkIndex, bulletEntity);
//                 // TODO: Tankダメージ処理
//                 break;
//             }
//         }
//     }
// }
