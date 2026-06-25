using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// AuraAttackRequest を消費し、範囲内の敵 Actor へ DamageEvent を積むシステム。
/// 判定部分は Projectile と同じく後で空間ハッシュへ差し替えられるよう、このシステムに閉じ込める。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(AttackRequestSystem))]
public partial struct AuraAttackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AuraAttackRequest>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var targetQuery = SystemAPI.QueryBuilder().WithAll<ActorBody, LocalTransform, Team, Hitbox>().Build();
        var targetEntities = targetQuery.ToEntityArray(state.WorldUpdateAllocator);
        var targetTransforms = targetQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
        var targetTeams = targetQuery.ToComponentDataArray<Team>(state.WorldUpdateAllocator);
        var targetHitboxes = targetQuery.ToComponentDataArray<Hitbox>(state.WorldUpdateAllocator);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (request, requestEntity) in
                 SystemAPI.Query<RefRO<AuraAttackRequest>>()
                     .WithEntityAccess())
        {
            var value = request.ValueRO;
            var radiusSq = value.Radius * value.Radius;

            for (var i = 0; i < targetEntities.Length; i++)
            {
                if (targetEntities[i] == value.Owner) continue;
                if (!TeamUtility.AreHostile(value.Team, targetTeams[i].Value)) continue;

                var hitRadius = value.Radius + targetHitboxes[i].Radius;
                var effectiveRadiusSq = math.max(radiusSq, hitRadius * hitRadius);
                if (math.distancesq(value.Position, targetTransforms[i].Position) > effectiveRadiusSq)
                {
                    continue;
                }

                ecb.AppendToBuffer(targetEntities[i], new DamageEvent
                {
                    Damage = value.Damage,
                    Attacker = value.Owner,
                });
            }

            ecb.DestroyEntity(requestEntity);
        }
    }
}
