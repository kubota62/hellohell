using Unity.Entities;
using Unity.Mathematics;

public struct Bullet : IComponentData
{
    public Entity Shooter;  // 発射者のEntity
    public float3 Velocity;
}
