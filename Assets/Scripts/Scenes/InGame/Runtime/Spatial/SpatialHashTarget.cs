using Unity.Entities;

/// <summary>
/// Marks entities that should be inserted into a spatial hash for broadphase queries.
/// The hash builder can stay generic by reading Team, Hitbox, and LocalTransform alongside this marker.
/// </summary>
public struct SpatialHashTarget : IComponentData
{
}
