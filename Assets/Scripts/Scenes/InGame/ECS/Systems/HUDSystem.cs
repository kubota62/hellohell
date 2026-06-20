using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct HUDSystem : ISystem
{
    EntityQuery tankCountQuery;
    EntityQuery projectileCountQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        tankCountQuery = SystemAPI.QueryBuilder().WithAll<Tank>().Build();
        projectileCountQuery = SystemAPI.QueryBuilder().WithAll<Projectile>().Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        // PlayMode テストでは、Canvas の Awake より先に ECS が更新されることがある。
        var hudBridge = HUDBridge.Instance;
        if (hudBridge == null)
        {
            return;
        }

        hudBridge.SetTankCount(tankCountQuery.CalculateEntityCount());
        hudBridge.SetBulletCount(projectileCountQuery.CalculateEntityCount());
    }
}
