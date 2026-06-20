using Unity.Entities;

/// <summary>
/// Singleton settings for grid-based broadphase.
/// CellSize should be at least the largest common hit radius to minimize neighbor checks.
/// </summary>
public struct SpatialHashSettings : IComponentData
{
    public float CellSize;
}
