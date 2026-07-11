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
[UpdateBefore(typeof(EnemyMovementForwardSystem))]
[UpdateBefore(typeof(EnemyMovementRandomSystem))]
[UpdateBefore(typeof(EnemyMovementKiteSystem))]
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
/// プレイヤー本体の向きとは独立して、タレットをマウスのワールド位置へ向ける。
/// ActorBodyAuthoring上でタレットは本体直下の子Transformとして設定する。
/// </summary>
[UpdateAfter(typeof(PlayerMovementSystem))]
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
