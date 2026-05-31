using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct TankMovementSystem : ISystem
{
    // コンポーネントへのランダムアクセス用ルックアップ
    private ComponentLookup<LocalTransform> m_LocalTransformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        // ルックアップの初期化
        m_LocalTransformLookup = state.GetComponentLookup<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        var job = new TankCombinedJob
        {
            DeltaTime = dt,
            // ここで「他のエンティティ（砲塔）を書き換えるよ」というコンポーネント情報を渡す
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false)
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Tank))]
[WithNone(typeof(Player))]
public partial struct TankCombinedJob : IJobEntity
{
    public float DeltaTime;
    
    // [NativeDisableParallelForRestriction] で「並列書き込み制限」を解除
    [NativeDisableParallelForRestriction] // 並列書き込みを許可
    [NativeDisableContainerSafetyRestriction] // エイリアシング（二重アクセス）エラーを回避
    public ComponentLookup<LocalTransform> TransformLookup;

    public void Execute(Entity entity, ref LocalTransform transform, in Tank tank)
    {
        // --- 移動処理 ---
        var pos = transform.Position;
        pos.y += (float)entity.Index;
        var angle = (0.5f + noise.cnoise(pos * 0.2f)) * math.PI * 2;
        var dir = float3.zero;
        math.sincos(angle, out dir.x, out dir.z);

        transform.Position += dir * DeltaTime * 2.0f;
        transform.Rotation = quaternion.RotateY(angle);

        // --- 砲塔の回転処理 ---
        if (TransformLookup.HasComponent(tank.Turret))
        {
            var spin = quaternion.RotateY(DeltaTime * math.PI);
            var turretTrans = TransformLookup[tank.Turret];
            turretTrans.Rotation = math.mul(spin, turretTrans.Rotation);
            
            // ここで書き込み！(上記の属性がないとエラーになる)
            TransformLookup[tank.Turret] = turretTrans;
        }
    }
}