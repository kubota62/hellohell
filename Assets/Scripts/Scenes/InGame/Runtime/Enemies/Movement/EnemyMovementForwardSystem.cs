using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// EnemyMovementForward を持つ Enemy を Player へまっすぐ接近させるシステム。
/// Player に重なる距離まで入った場合は、最低距離を保つように押し戻す。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
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
/// 横方向のレーン差を少し入れて、敵同士が完全に重なりにくい接近軌道にする。
/// </summary>
[BurstCompile]
[WithAll(typeof(ActorBody))]
[WithAll(typeof(Enemy))]
[WithAll(typeof(EnemyMovementForward))]
[WithNone(typeof(Player))]
public partial struct EnemyMovementForwardJob : IJobEntity
{
    const float PersonalSpace = 3.5f;
    const float PushBackSpeed = 4f;

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
            transform.Position += new float3(1f, 0f, 0f) * PushBackSpeed * DeltaTime;
            return;
        }

        var forward = toPlayer / distance;
        if (distance < PersonalSpace)
        {
            transform.Position -= forward * PushBackSpeed * DeltaTime;
            transform.Rotation = quaternion.LookRotationSafe(forward, math.up());
            RotateTurret(actorBody, DeltaTime);
            return;
        }

        var tangent = new float3(-forward.z, 0f, forward.x);
        var laneOffset = ((entity.Index % 7) - 3) * 0.04f;
        var desiredDirection = math.normalizesafe(forward + tangent * laneOffset, forward);

        var speed = math.lerp(1.6f, 2.8f, math.saturate(distance / 18f));
        if (distance < 6f)
        {
            speed *= math.saturate((distance - PersonalSpace) / (6f - PersonalSpace));
        }

        transform.Position += desiredDirection * speed * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(desiredDirection, math.up());

        RotateTurret(actorBody, DeltaTime);
    }

    private void RotateTurret(in ActorBody actorBody, float deltaTime)
    {
        // 接近中も敵のシルエットが読めるように砲塔をゆっくり回転させる。
        if (TransformLookup.HasComponent(actorBody.Turret))
        {
            var spin = quaternion.RotateY(deltaTime * math.PI);
            var turretTrans = TransformLookup[actorBody.Turret];
            turretTrans.Rotation = math.mul(spin, turretTrans.Rotation);
            TransformLookup[actorBody.Turret] = turretTrans;
        }
    }
}
