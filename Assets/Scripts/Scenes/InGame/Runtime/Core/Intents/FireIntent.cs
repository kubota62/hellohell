using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Non-structural fire intent used by input and AI.
/// This avoids adding/removing request tags every frame for large crowds.
/// </summary>
public struct FireIntent : IComponentData
{
    public bool IsPressed;
    public float3 Origin;
    public float3 Direction;
}
