using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// ActorBodyに積まれたDamageEventを集計し、Healthへ反映するシステム。
/// 表示や効果音などの演出はVfxRequestとして別Systemへ渡す。
/// </summary>
[BurstCompile]
public partial struct ActorDamageEventSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, health, damageEventBuffer) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<Health>, DynamicBuffer<DamageEvent>>()
                     .WithAll<ActorBody>())
        {
            var pos = transform.ValueRO.Position + new float3(0f, 1f, 0f);
            var totalDamage = 0;

            // このフレームに溜まったダメージをまとめて減算し、各ヒットの表示だけ別リクエストへ逃がす。
            foreach (var damage in damageEventBuffer)
            {
                totalDamage += damage.Damage;

                var requestEntity = ecb.CreateEntity();
                ecb.AddComponent(requestEntity, new VfxRequest
                {
                    Kind = VfxRequestKind.DamageDigit,
                    IntValue = damage.Damage,
                    Position = pos,
                });
            }

            health.ValueRW.Current = math.max(0, health.ValueRO.Current - totalDamage);
            damageEventBuffer.Clear();
        }
    }
}

/// <summary>
/// ゲームロジックから演出層へ渡す、使い捨ての演出リクエスト。
/// ダメージ、死亡、攻撃発生などの見た目をロジック本体から分離するために使う。
/// </summary>
public struct VfxRequest : IComponentData
{
    public VfxRequestKind Kind;
    public int IntValue;
    public float3 Position;
}

/// <summary>
/// VfxRequestSystemが処理できる演出の種類。
/// </summary>
public enum VfxRequestKind : byte
{
    DamageDigit = 1,
}

/// <summary>
/// VfxRequestを消費し、実際の演出エンティティ生成へ変換するシステム。
/// ロジック側は演出Prefabや表示方式を知らず、このSystemが演出の窓口になる。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ActorDamageEventSystem))]
public partial struct VfxRequestSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<VfxRequest>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (request, entity) in
                 SystemAPI.Query<RefRO<VfxRequest>>()
                     .WithEntityAccess())
        {
            if (request.ValueRO.Kind == VfxRequestKind.DamageDigit)
            {
                DamageDigitSpawnUtility.SpawnDamageDigits(
                    ecb,
                    config.DamageDigitPrefab,
                    request.ValueRO.IntValue,
                    request.ValueRO.Position);
            }

            ecb.DestroyEntity(entity);
        }
    }
}
