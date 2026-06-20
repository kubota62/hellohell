using Unity.Entities;

/// <summary>
/// Shared projectile metadata for hit filtering and damage resolution.
/// Movement remains separate so different projectile motion models can coexist.
/// </summary>
public struct Projectile : IComponentData
{
    public Entity Owner;
    public TeamId Team;
    public int Damage;
    public float HitRadius;
}
