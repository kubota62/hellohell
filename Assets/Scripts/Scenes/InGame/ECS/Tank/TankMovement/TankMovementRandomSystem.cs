using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// TankMovementRandom を持つ敵タンクを Player へ接近させつつ、ノイズで横方向に揺らすシステム。
/// Player 自身は Job 側の WithNone(Player) で対象から外し、操作入力による移動と分離する。
/// </summary>
[BurstCompile]
public partial struct TankMovementRandomSystem : ISystem
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

        var job = new TankMovementRandomJob
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
/// ランダム型の敵タンクを移動させる Job。
/// 時間変化するノイズで回り込み方向を作り、単純な直進敵と違う動きに見せる。
/// </summary>
[BurstCompile]
[WithAll(typeof(Tank))]
[WithAll(typeof(Enemy))]
[WithAll(typeof(TankMovementRandom))]
[WithNone(typeof(Player))]
public partial struct TankMovementRandomJob
    : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;
    public float ElapsedTime;
    
    // 各タンクは自分の子階層の砲塔だけを書き換える前提なので、並列書き込みを許可する。
    [NativeDisableParallelForRestriction]
    [NativeDisableContainerSafetyRestriction]
    public ComponentLookup<LocalTransform> TransformLookup;

    [BurstCompile]
    public void Execute(Entity entity, ref LocalTransform transform, in Tank tank)
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
        var weave = noise.cnoise(new float3(
            transform.Position.x * 0.12f + entity.Index * 0.17f,
            transform.Position.z * 0.12f,
            ElapsedTime * 0.35f));

        // プレイヤーへ接近しながら横方向へ流し、ゆるい包囲を作る。
        var encircle = tangent * weave * 0.65f;
        var desiredDirection = math.normalizesafe(forward + encircle, forward);

        var speed = math.lerp(1.3f, 2.4f, math.saturate(distance / 16f));
        if (distance < 2.5f)
        {
            speed *= 0.25f;
        }

        transform.Position += desiredDirection * speed * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(desiredDirection, math.up());

        // 直進型と見分けやすいシルエットになるように砲塔を回転させる。
        if (TransformLookup.HasComponent(tank.Turret))
        {
            var spin = quaternion.RotateY(DeltaTime * math.PI);
            var turretTrans = TransformLookup[tank.Turret];
            turretTrans.Rotation = math.mul(spin, turretTrans.Rotation);
            TransformLookup[tank.Turret] = turretTrans;
        }
    }
}
