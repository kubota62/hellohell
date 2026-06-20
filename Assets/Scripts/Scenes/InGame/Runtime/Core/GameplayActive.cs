using Unity.Entities;

/// <summary>
/// Enableable marker for pooled entities that should currently participate in gameplay.
/// Prefer toggling this over structural Add/Remove when objects are reused frequently.
/// </summary>
public struct GameplayActive : IComponentData, IEnableableComponent
{
}
