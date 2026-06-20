using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// EnemyMovementForward を持つ Enemy を Player へまっすぐ接近させるシステム。
/// Player 自身は Job 側の WithNone(Player) で対象から外し、操作入力による移動と分離する。
/// </summary>
[BurstCompile]
public partial struct EnemyMovementForwardSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<Player>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        var dt = SystemAPI.Time.DeltaTime;

        var job = new EnemyMovementForwardJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = dt,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false)
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// 直進型の Enemy を移動させる Job。
/// 少しだけ横方向のレーン差を入れて、敵同士が完全に重なりにくい接近軌道にする。
/// </summary>
[BurstCompile]
[WithAll(typeof(ActorBody))]
[WithAll(typeof(Enemy))]
[WithAll(typeof(EnemyMovementForward))]
[WithNone(typeof(Player))]
public partial struct EnemyMovementForwardJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;

    // 各 ActorBody は自分の子階層の砲塔だけを書き換える前提なので、並列書き込みを許可する。
    [NativeDisableParallelForRestriction]
    [NativeDisableContainerSafetyRestriction]
    public ComponentLookup<LocalTransform> TransformLookup;

    [BurstCompile]
    public void Execute(Entity entity, ref LocalTransform transform, in ActorBody actorBody)
    {
        var toPlayer = PlayerPosition - transform.Position;
        toPlayer.y = 0f;

        var distance = math.length(toPlayer);
        if (distance < 0.001f)
        {
            return;
        }

        var forward = toPlayer / distance;
        var tangent = new float3(-forward.z, 0f, forward.x);
        var laneOffset = ((entity.Index % 7) - 3) * 0.04f;
        var desiredDirection = math.normalizesafe(forward + tangent * laneOffset, forward);

        var speed = math.lerp(1.6f, 2.8f, math.saturate(distance / 18f));
        if (distance < 2.0f)
        {
            speed *= 0.35f;
        }

        transform.Position += desiredDirection * speed * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(desiredDirection, math.up());

        // 接近中も敵のシルエットが読めるように砲塔を回転させる。
        if (TransformLookup.HasComponent(actorBody.Turret))
        {
            var spin = quaternion.RotateY(DeltaTime * math.PI);
            var turretTrans = TransformLookup[actorBody.Turret];
            turretTrans.Rotation = math.mul(spin, turretTrans.Rotation);
            TransformLookup[actorBody.Turret] = turretTrans;
        }
    }
}
