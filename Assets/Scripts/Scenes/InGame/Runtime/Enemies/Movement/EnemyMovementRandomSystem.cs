using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// EnemyMovementRandom を持つ Enemy を Player へ接近させつつ、ノイズで横方向に揺らすシステム。
/// Player に重なる距離まで入った場合は、最低距離を保つように押し戻す。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TransformSystemGroup))]
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
        var dt = SystemAPI.Time.DeltaTime;

        var job = new EnemyMovementRandomJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = dt,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false)
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// 揺れながら接近する Enemy を移動させる Job。
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
        var weave = noise.cnoise(new float3(
            transform.Position.x * 0.12f + entity.Index * 0.17f,
            transform.Position.z * 0.12f,
            ElapsedTime * 0.35f));

        var encircle = tangent * weave * 0.65f;
        var desiredDirection = math.normalizesafe(forward + encircle, forward);

        var speed = math.lerp(1.3f, 2.4f, math.saturate(distance / 16f));
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
