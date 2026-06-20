using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Non-structural movement intent shared by player input and enemy AI.
/// Producers update the values; movement systems apply them.
/// </summary>
public struct MoveIntent : IComponentData
{
    public float3 Direction;
    public float Magnitude;
}
