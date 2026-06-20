using Unity.Entities;

/// <summary>
/// Simple spherical hit volume used by spatial hash based combat queries.
/// Radius is measured in world units around the entity LocalTransform position.
/// </summary>
public struct Hitbox : IComponentData
{
    public float Radius;
}
