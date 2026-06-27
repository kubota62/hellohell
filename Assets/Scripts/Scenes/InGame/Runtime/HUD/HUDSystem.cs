using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 現在生存している ActorBody と Projectile の数を集計し、Managed 側の HUD へ渡す。
/// Canvas の初期化より ECS 更新が先に走る場合があるため、HUDBridge が無ければ何もしない。
/// </summary>
[BurstCompile]
public partial struct HUDSystem : ISystem
{
    EntityQuery actorCountQuery;
    EntityQuery projectileCountQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        actorCountQuery = SystemAPI.QueryBuilder().WithAll<ActorBody>().Build();
        projectileCountQuery = SystemAPI.QueryBuilder().WithAll<Projectile>().Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        var hudBridge = HUDBridge.Instance;
        if (hudBridge == null)
        {
            return;
        }

        hudBridge.SetActorCount(actorCountQuery.CalculateEntityCount());
        hudBridge.SetProjectileCount(projectileCountQuery.CalculateEntityCount());
    }
}
