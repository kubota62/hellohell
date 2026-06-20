using Unity.Entities;

/// <summary>
/// Remaining lifetime in seconds for temporary entities such as projectiles and VFX requests.
/// Pool-aware systems can later disable entities instead of destroying them when this reaches zero.
/// </summary>
public struct Lifetime : IComponentData
{
    public float Remaining;
}
