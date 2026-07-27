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
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var hasPlayerSkillStats = SystemAPI.TryGetSingleton<PlayerSkillStats>(
            out var playerSkillStats);

        foreach (var (transform, health, damageEventBuffer, actorEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<Health>, DynamicBuffer<DamageEvent>>()
                     .WithAll<ActorBody>()
                     .WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PlayerRevivalGrace>(actorEntity))
            {
                damageEventBuffer.Clear();
                continue;
            }

            var pos = transform.ValueRO.Position + new float3(0f, 1f, 0f);
            var remainingHealth = health.ValueRO.Current;
            var damageReduction =
                SystemAPI.HasComponent<Player>(actorEntity) &&
                SystemAPI.HasComponent<PlayerSkillStats>(actorEntity)
                    ? PlayerAutoSkillSystem.GetDamageReduction(
                        SystemAPI.GetComponent<PlayerSkillStats>(actorEntity))
                    : 0f;
            var executionDamageBonus =
                hasPlayerSkillStats &&
                SystemAPI.HasComponent<Enemy>(actorEntity)
                    ? PlayerAutoSkillSystem.GetExecutionDamageBonus(
                        playerSkillStats)
                    : 0f;

            // このフレームに溜まったダメージをまとめて減算し、各ヒットの表示だけ別リクエストへ逃がす。
            foreach (var damage in damageEventBuffer)
            {
                var executionDamage = ResolveExecutionDamage(
                    damage.Damage,
                    remainingHealth,
                    health.ValueRO.Max,
                    executionDamageBonus);
                var resolvedDamage = ResolveIncomingDamage(
                    executionDamage,
                    damageReduction);
                remainingHealth = math.max(0, remainingHealth - resolvedDamage);

                var requestEntity = ecb.CreateEntity();
                ecb.AddComponent(requestEntity, new VfxRequest
                {
                    Kind = VfxRequestKind.DamageDigit,
                    IntValue = resolvedDamage,
                    Position = pos,
                    IsCritical = damage.IsCritical,
                });
            }

            health.ValueRW.Current = remainingHealth;
            damageEventBuffer.Clear();
        }
    }

    public static int ResolveIncomingDamage(int damage, float reduction)
    {
        if (damage <= 0)
        {
            return 0;
        }

        var safeReduction = math.clamp(reduction, 0f, 0.65f);
        return math.max(
            1,
            (int)math.round(damage * (1f - safeReduction)));
    }

    public static int ResolveExecutionDamage(
        int damage,
        int currentHealth,
        int maxHealth,
        float damageBonus)
    {
        if (damage <= 0)
        {
            return 0;
        }

        if (maxHealth <= 0 ||
            math.clamp(currentHealth / (float)maxHealth, 0f, 1f) > 0.3f)
        {
            return damage;
        }

        var safeBonus = math.clamp(damageBonus, 0f, 1.5f);
        return math.max(
            1,
            (int)math.round(damage * (1f + safeBonus)));
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
    public byte IsCritical;
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
                    request.ValueRO.Position,
                    request.ValueRO.IsCritical != 0);
            }

            ecb.DestroyEntity(entity);
        }
    }
}
