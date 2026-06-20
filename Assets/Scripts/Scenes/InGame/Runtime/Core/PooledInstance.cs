using Unity.Entities;

/// <summary>
/// Marks an entity as eligible for future pool reuse and records the prefab it came from.
/// Step 0 only defines the contract; pool storage can be added once spawn pressure appears.
/// </summary>
public struct PooledInstance : IComponentData
{
    public Entity SourcePrefab;
}
