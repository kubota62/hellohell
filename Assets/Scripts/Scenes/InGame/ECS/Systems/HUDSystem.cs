using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct HUDSystem : ISystem
{
    EntityQuery tankCountQuery;
    EntityQuery bulletCountQuery;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        tankCountQuery = SystemAPI.QueryBuilder().WithAll<Tank>().Build();
        bulletCountQuery = SystemAPI.QueryBuilder().WithAll<Bullet>().Build();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        HUDBridge.Instance.SetTankCount(tankCountQuery.CalculateEntityCount());
        HUDBridge.Instance.SetBulletCount(bulletCountQuery.CalculateEntityCount());
    }
}