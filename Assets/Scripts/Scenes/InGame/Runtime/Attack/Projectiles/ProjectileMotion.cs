using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Motion state for projectile-style attacks.
/// Projectile contains combat metadata; this component contains movement data.
/// </summary>
public struct ProjectileMotion : IComponentData
{
    public Entity Shooter;
    public float3 Velocity;
}
