using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Player/Variants/Movement/Default のWASD移動システム。
/// 共有の PlayerInput エンティティを読み、Player の ActorBody を移動させる。
/// LocalTransform の変更を同フレームで LocalToWorld に反映するため、TransformSystemGroup より前に実行する。
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(EnemyMovementSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            runState.IsGameOver != 0)
        {
            return;
        }

        var input = SystemAPI.GetSingleton<PlayerInput>();
        if (math.lengthsq(input.Movement) < 0.0001f)
        {
            return;
        }

        var direction = math.normalizesafe(new float3(input.Movement.x, 0f, input.Movement.y));
        var moveSpeed = ResolveMoveSpeed(ref state);
        if (SystemAPI.TryGetSingleton<PlayerSkillStats>(out var skillStats))
        {
            moveSpeed *= PlayerAutoSkillSystem.GetMoveSpeedMultiplier(skillStats);
        }

        var job = new PlayerMovementJob
        {
            Direction = direction,
            DeltaTime = SystemAPI.Time.DeltaTime,
            Speed = moveSpeed
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    private float ResolveMoveSpeed(ref SystemState state)
    {
        // Playerの基礎移動速度はマスタから読む。スキルによる倍率はOnUpdate側で最後に掛ける。
        if (SystemAPI.TryGetSingletonBuffer<PlayerMasterElement>(out var playerMasters, true))
        {
            return PlayerMasterCatalog.Get(playerMasters, PlayerMasterId.Default).MoveSpeed;
        }

        return PlayerMasterCatalog.Get(PlayerMasterId.Default).MoveSpeed;
    }
}

/// <summary>
/// Default移動方式で、Player タグを持つ ActorBody だけを移動させるJob。
/// 入力値はスケジュール前に XZ 平面上のワールド方向へ変換しておく。
/// </summary>
[BurstCompile]
[WithAll(typeof(Player))]
[WithNone(typeof(PlayerDefeated))]
public partial struct PlayerMovementJob : IJobEntity
{
    public float3 Direction;
    public float DeltaTime;
    public float Speed;

    [BurstCompile]
    public void Execute(ref LocalTransform transform)
    {
        transform.Position += Direction * Speed * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(Direction, math.up());
    }
}

/// <summary>
/// オート攻撃中は最寄りの生存Enemyを照準位置に設定し、攻撃入力を自動で有効にする。
/// </summary>
[UpdateAfter(typeof(PlayerMovementSystem))]
[UpdateBefore(typeof(PlayerTurretAimSystem))]
[UpdateBefore(typeof(AttackRequestSystem))]
public partial struct PlayerAutoAimSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<PlayerInput>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var input = SystemAPI.GetSingletonRW<PlayerInput>();
        if (SystemAPI.HasSingleton<PlayerDefeated>())
        {
            input.ValueRW.IsFire = false;
            input.ValueRW.HasAimPosition = false;
            return;
        }

        if (!input.ValueRO.AutoAttackEnabled)
        {
            return;
        }

        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        var nearestDistanceSq = float.MaxValue;
        var targetPosition = float3.zero;
        var foundTarget = false;

        foreach (var (transform, health) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Health>>()
                     .WithAll<Enemy, GameplayActive>())
        {
            if (health.ValueRO.Current <= 0)
            {
                continue;
            }

            var distanceSq = math.distancesq(playerPosition, transform.ValueRO.Position);
            if (distanceSq >= nearestDistanceSq)
            {
                continue;
            }

            nearestDistanceSq = distanceSq;
            targetPosition = transform.ValueRO.Position;
            foundTarget = true;
        }

        input.ValueRW.IsFire = foundTarget;
        input.ValueRW.HasAimPosition = foundTarget;
        if (foundTarget)
        {
            input.ValueRW.AimWorldPosition = targetPosition;
        }
    }
}

/// <summary>
/// プレイヤー本体の向きとは独立して、タレットをマウスのワールド位置へ向ける。
/// ActorBodyAuthoring上でタレットは本体直下の子Transformとして設定する。
/// </summary>
[UpdateAfter(typeof(PlayerMovementSystem))]
[UpdateAfter(typeof(PlayerAutoAimSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerTurretAimSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<PlayerInput>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();
        if (SystemAPI.HasSingleton<PlayerDefeated>())
        {
            return;
        }

        if (!input.HasAimPosition)
        {
            return;
        }

        foreach (var (body, actorTransform) in
                 SystemAPI.Query<RefRO<ActorBody>, RefRO<LocalTransform>>()
                     .WithAll<Player>())
        {
            var turretEntity = body.ValueRO.Turret;
            if (turretEntity == Entity.Null ||
                !state.EntityManager.Exists(turretEntity) ||
                !state.EntityManager.HasComponent<LocalTransform>(turretEntity))
            {
                continue;
            }

            var worldDirection = input.AimWorldPosition - actorTransform.ValueRO.Position;
            worldDirection.y = 0f;
            if (math.lengthsq(worldDirection) < 0.0001f)
            {
                continue;
            }

            var localDirection = math.rotate(
                math.inverse(actorTransform.ValueRO.Rotation),
                math.normalizesafe(worldDirection));
            localDirection.y = 0f;

            var turretTransform = state.EntityManager.GetComponentData<LocalTransform>(turretEntity);
            turretTransform.Rotation = quaternion.LookRotationSafe(localDirection, math.up());
            state.EntityManager.SetComponentData(turretEntity, turretTransform);
        }
    }
}
