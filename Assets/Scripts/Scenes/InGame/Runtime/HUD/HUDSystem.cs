using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 現在生存している ActorBody と active な Projectile の数を集計し、Managed 側の HUD へ渡す。
/// inactive なプール待機弾は Projectile 数に含めない。
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
        projectileCountQuery = SystemAPI.QueryBuilder().WithAll<Projectile, GameplayActive>().Build();
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
