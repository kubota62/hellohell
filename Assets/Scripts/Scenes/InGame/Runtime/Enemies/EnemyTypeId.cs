using Unity.Entities;

/// <summary>
/// Lightweight reference to an enemy definition.
/// A later EnemyDefinition baker can map this id to stats, prefab choices, weapons, and rewards.
/// </summary>
public struct EnemyTypeId : IComponentData
{
    public int Value;
}
