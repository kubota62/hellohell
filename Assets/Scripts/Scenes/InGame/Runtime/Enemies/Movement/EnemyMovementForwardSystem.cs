using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// EnemyMovementForward を持つ Enemy の移動意図を作るシステム。
/// 実際の座標更新は EnemyMoveIntentApplySystem に集約する。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
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

        var job = new EnemyMovementForwardJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = SystemAPI.Time.DeltaTime,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false)
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// 直進型 Enemy の移動方向と速度を MoveIntent に書き込む Job。
/// 敵同士が完全に重なりにくいよう、わずかなレーン差もここで作る。
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

    [NativeDisableParallelForRestriction]
    [NativeDisableContainerSafetyRestriction]
    public ComponentLookup<LocalTransform> TransformLookup;

    [BurstCompile]
    public void Execute(
        Entity entity,
        in LocalTransform transform,
        in ActorBody actorBody,
        ref MoveIntent moveIntent)
    {
        var toPlayer = PlayerPosition - transform.Position;
        toPlayer.y = 0f;

        var distance = math.length(toPlayer);
        if (distance < 0.001f)
        {
            moveIntent.Direction = new float3(1f, 0f, 0f);
            moveIntent.Magnitude = PushBackSpeed;
            return;
        }

        var forward = toPlayer / distance;
        if (distance < PersonalSpace)
        {
            moveIntent.Direction = -forward;
            moveIntent.Magnitude = PushBackSpeed;
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

        moveIntent.Direction = desiredDirection;
        moveIntent.Magnitude = speed;
        RotateTurret(actorBody, DeltaTime);
    }

    private void RotateTurret(in ActorBody actorBody, float deltaTime)
    {
        // 接近中も敵のシルエットが読めるように砲塔をゆっくり回す。
        if (TransformLookup.HasComponent(actorBody.Turret))
        {
            var spin = quaternion.RotateY(deltaTime * math.PI);
            var turretTrans = TransformLookup[actorBody.Turret];
            turretTrans.Rotation = math.mul(spin, turretTrans.Rotation);
            TransformLookup[actorBody.Turret] = turretTrans;
        }
    }
}

/// <summary>
/// Enemy の MoveIntent を実際の移動と向きへ反映するシステム。
/// AIごとの判断と移動適用を分け、敵種類を増やしても共通処理を再利用する。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveIntentApplySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new EnemyMoveIntentApplyJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(ActorBody))]
[WithAll(typeof(Enemy))]
[WithNone(typeof(Player))]
public partial struct EnemyMoveIntentApplyJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, ref MoveIntent moveIntent)
    {
        if (moveIntent.Magnitude <= 0f || math.lengthsq(moveIntent.Direction) < 0.0001f)
        {
            moveIntent.Direction = float3.zero;
            moveIntent.Magnitude = 0f;
            return;
        }

        var direction = math.normalizesafe(moveIntent.Direction);
        transform.Position += direction * moveIntent.Magnitude * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(direction, math.up());

        moveIntent.Direction = float3.zero;
        moveIntent.Magnitude = 0f;
    }
}
