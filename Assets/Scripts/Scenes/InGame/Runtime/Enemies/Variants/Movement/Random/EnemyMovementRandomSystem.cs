using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Enemies/Variants/Movement/Random の移動意思生成システム。
/// EnemyMovementRandom を持つ Enemy の移動意図を作る。
/// ノイズで横方向に揺らしつつ、座標更新は共通の適用システムへ任せる。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EnemyMovementRandomSystem : ISystem
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

        var job = new EnemyMovementRandomJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false)
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// 揺れながら接近する Enemy の移動方向と速度を MoveIntent に書き込む Job。
/// ノイズで回り込み方向を作り、単純な直進敵と違う動きに見せる。
/// </summary>
[BurstCompile]
[WithAll(typeof(ActorBody))]
[WithAll(typeof(Enemy))]
[WithAll(typeof(EnemyMovementRandom))]
[WithNone(typeof(Player))]
public partial struct EnemyMovementRandomJob : IJobEntity
{
    const float PersonalSpace = 4f;
    const float PushBackSpeed = 4f;

    public float3 PlayerPosition;
    public float DeltaTime;
    public float ElapsedTime;

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
        var weave = noise.cnoise(new float3(
            transform.Position.x * 0.12f + entity.Index * 0.17f,
            transform.Position.z * 0.12f,
            ElapsedTime * 0.35f));

        var encircle = tangent * weave * 0.65f;
        var desiredDirection = math.normalizesafe(forward + encircle, forward);

        var speed = math.lerp(moveSpeed.Value * 0.54f, moveSpeed.Value, math.saturate(distance / 16f));
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
        // 直進型と見分けやすいシルエットになるように砲塔を回転させる。
        if (TransformLookup.HasComponent(actorBody.Turret))
        {
            var spin = quaternion.RotateY(deltaTime * math.PI);
            var turretTrans = TransformLookup[actorBody.Turret];
            turretTrans.Rotation = math.mul(spin, turretTrans.Rotation);
            TransformLookup[actorBody.Turret] = turretTrans;
        }
    }
}
