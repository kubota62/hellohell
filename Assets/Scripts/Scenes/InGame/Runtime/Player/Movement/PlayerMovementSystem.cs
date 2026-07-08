using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
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
/// Player タグを持つ ActorBody だけを移動させる。
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
