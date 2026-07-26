using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 全Enemyの移動意思を生成する。どのパターンもPlayerへ接近し、後退はしない。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(EnemyMoveIntentApplySystem))]
public partial struct EnemyMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        var job = new EnemyMovementJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Enemy), typeof(ActorBody), typeof(EnemyMovementPattern))]
[WithNone(typeof(Player))]
public partial struct EnemyMovementJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;
    public float ElapsedTime;

    [NativeDisableParallelForRestriction]
    [NativeDisableContainerSafetyRestriction]
    public ComponentLookup<LocalTransform> TransformLookup;

    [BurstCompile]
    private void Execute(
        Entity entity,
        in LocalTransform transform,
        in ActorBody actorBody,
        in EnemyMovementPattern pattern,
        in EnemyMoveSpeed moveSpeed,
        ref MoveIntent moveIntent)
    {
        var forward = PlayerPosition - transform.Position;
        forward.y = 0f;

        if (math.lengthsq(forward) < 0.0001f)
        {
            moveIntent.Direction = float3.zero;
            moveIntent.Magnitude = 0f;
            return;
        }

        forward = math.normalize(forward);
        var tangent = new float3(-forward.z, 0f, forward.x);
        var lateralOffset = GetLateralOffset(entity, pattern.Kind);

        moveIntent.Direction = math.normalizesafe(
            forward + tangent * lateralOffset,
            forward);
        moveIntent.Magnitude = moveSpeed.Value;

        RotateTurret(actorBody.Turret);
    }

    private float GetLateralOffset(Entity entity, EnemyMovementKind kind)
    {
        switch (kind)
        {
            case EnemyMovementKind.Drift:
            case EnemyMovementKind.Random:
                return math.sin(ElapsedTime * 1.3f + entity.Index * 1.7f) * 0.16f;

            case EnemyMovementKind.Runner:
                return math.sin(ElapsedTime * 4f + entity.Index * 2.1f) * 0.08f;

            case EnemyMovementKind.Heavy:
                return ((entity.Index % 5) - 2) * 0.025f;

            case EnemyMovementKind.Kite:
            case EnemyMovementKind.Forward:
            default:
                return ((entity.Index % 7) - 3) * 0.04f;
        }
    }

    private void RotateTurret(Entity turretEntity)
    {
        if (!TransformLookup.HasComponent(turretEntity))
        {
            return;
        }

        var turretTransform = TransformLookup[turretEntity];
        turretTransform.Rotation = math.mul(
            quaternion.RotateY(DeltaTime * math.PI),
            turretTransform.Rotation);
        TransformLookup[turretEntity] = turretTransform;
    }
}
