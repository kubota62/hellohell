using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Enemies/Variants/Movement/Forward の移動意思生成システム。
/// EnemyMovementForward を持つ Enemy を Player へ直線的に接近させる。
/// 実際の座標更新は Enemies/Core/Movement の EnemyMoveIntentApplySystem に集約する。
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
        in EnemyMoveSpeed moveSpeed,
        ref MoveIntent moveIntent)
    {
        var toPlayer = PlayerPosition - transform.Position;
        toPlayer.y = 0f;

        var distance = math.length(toPlayer);
        if (distance < 0.001f)
        {
            moveIntent.Direction = float3.zero;
            moveIntent.Magnitude = 0f;
            return;
        }

        var forward = toPlayer / distance;
        var tangent = new float3(-forward.z, 0f, forward.x);
        var laneOffset = ((entity.Index % 7) - 3) * 0.04f;
        var desiredDirection = math.normalizesafe(forward + tangent * laneOffset, forward);

        moveIntent.Direction = desiredDirection;
        moveIntent.Magnitude = moveSpeed.Value;
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

public struct EnemyMovementDrift : IComponentData
{
}

public struct EnemyMovementRunner : IComponentData
{
}

public struct EnemyMovementHeavy : IComponentData
{
}

[BurstCompile]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EnemyMovementDriftSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(
            SystemAPI.GetSingletonEntity<Player>()).Position;
        state.Dependency = new EnemyMovementDriftJob
        {
            PlayerPosition = playerPosition,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Enemy), typeof(EnemyMovementDrift))]
public partial struct EnemyMovementDriftJob : IJobEntity
{
    public float3 PlayerPosition;
    public float ElapsedTime;

    private void Execute(
        Entity entity,
        in LocalTransform transform,
        in EnemyMoveSpeed moveSpeed,
        ref MoveIntent moveIntent)
    {
        var forward = PlayerPosition - transform.Position;
        forward.y = 0f;
        forward = math.normalizesafe(forward);
        var tangent = new float3(-forward.z, 0f, forward.x);
        var wave = math.sin(ElapsedTime * 1.3f + entity.Index * 1.7f);

        moveIntent.Direction = math.normalizesafe(forward + tangent * wave * 0.16f, forward);
        moveIntent.Magnitude = moveSpeed.Value;
    }
}

[BurstCompile]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EnemyMovementRunnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(
            SystemAPI.GetSingletonEntity<Player>()).Position;
        state.Dependency = new EnemyMovementRunnerJob
        {
            PlayerPosition = playerPosition,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Enemy), typeof(EnemyMovementRunner))]
public partial struct EnemyMovementRunnerJob : IJobEntity
{
    public float3 PlayerPosition;
    public float ElapsedTime;

    private void Execute(
        Entity entity,
        in LocalTransform transform,
        in EnemyMoveSpeed moveSpeed,
        ref MoveIntent moveIntent)
    {
        var forward = PlayerPosition - transform.Position;
        forward.y = 0f;
        forward = math.normalizesafe(forward);
        var tangent = new float3(-forward.z, 0f, forward.x);
        var scurry = math.sin(ElapsedTime * 4f + entity.Index * 2.1f);

        moveIntent.Direction = math.normalizesafe(forward + tangent * scurry * 0.08f, forward);
        moveIntent.Magnitude = moveSpeed.Value;
    }
}

[BurstCompile]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EnemyMovementHeavySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(
            SystemAPI.GetSingletonEntity<Player>()).Position;
        state.Dependency = new EnemyMovementHeavyJob
        {
            PlayerPosition = playerPosition,
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Enemy), typeof(EnemyMovementHeavy))]
public partial struct EnemyMovementHeavyJob : IJobEntity
{
    public float3 PlayerPosition;

    private void Execute(
        Entity entity,
        in LocalTransform transform,
        in EnemyMoveSpeed moveSpeed,
        ref MoveIntent moveIntent)
    {
        var forward = PlayerPosition - transform.Position;
        forward.y = 0f;
        forward = math.normalizesafe(forward);
        var tangent = new float3(-forward.z, 0f, forward.x);
        var laneOffset = ((entity.Index % 5) - 2) * 0.025f;

        moveIntent.Direction = math.normalizesafe(forward + tangent * laneOffset, forward);
        moveIntent.Magnitude = moveSpeed.Value;
    }
}
