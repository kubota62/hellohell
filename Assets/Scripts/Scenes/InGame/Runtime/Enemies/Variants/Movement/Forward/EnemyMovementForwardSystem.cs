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
[WithNone(typeof(Player), typeof(EnemyAttackWindup))]
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
        in EnemyAttackRange attackRange,
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

        var distance = math.length(forward);
        forward /= distance;
        var tangent = new float3(-forward.z, 0f, forward.x);
        var lateralOffset = GetLateralOffset(entity, pattern.Kind);

        if (pattern.Kind == EnemyMovementKind.Kite)
        {
            var radialIntent = EnemyKiteMovementUtility.ResolveRadialIntent(
                distance,
                attackRange.Min,
                attackRange.Max);
            moveIntent.Direction =
                EnemyKiteMovementUtility.ResolveDirection(
                    forward,
                    tangent,
                    lateralOffset,
                    radialIntent);
            moveIntent.Magnitude = moveSpeed.Value *
                EnemyKiteMovementUtility.ResolveSpeedMultiplier(
                    radialIntent);
            RotateTurret(actorBody.Turret);
            return;
        }

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
                return (entity.Index & 1) == 0 ? 1f : -1f;

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

public static class EnemyKiteMovementUtility
{
    public static int ResolveRadialIntent(
        float distance,
        float minimumAttackRange,
        float maximumAttackRange)
    {
        var safeMinimum = math.max(0f, minimumAttackRange);
        var safeMaximum = math.max(safeMinimum + 1f, maximumAttackRange);
        var retreatDistance = safeMinimum + 0.75f;
        var approachDistance = math.max(
            retreatDistance + 1f,
            safeMaximum * 0.82f);

        if (distance < retreatDistance)
        {
            return -1;
        }

        return distance > approachDistance ? 1 : 0;
    }

    public static float3 ResolveDirection(
        float3 forward,
        float3 tangent,
        float orbitSide,
        int radialIntent)
    {
        var safeForward = math.normalizesafe(
            forward,
            new float3(0f, 0f, 1f));
        var safeTangent = math.normalizesafe(
            tangent,
            new float3(-safeForward.z, 0f, safeForward.x));
        var side = orbitSide >= 0f ? 1f : -1f;
        if (radialIntent < 0)
        {
            return math.normalizesafe(
                -safeForward + safeTangent * side * 0.42f,
                -safeForward);
        }

        if (radialIntent > 0)
        {
            return math.normalizesafe(
                safeForward + safeTangent * side * 0.18f,
                safeForward);
        }

        return safeTangent * side;
    }

    public static float ResolveSpeedMultiplier(int radialIntent)
    {
        return radialIntent == 0 ? 0.78f : 1f;
    }
}
