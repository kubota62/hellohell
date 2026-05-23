using Unity.Entities;
using Unity.Mathematics;

public struct PlayerInput : IComponentData
{
    public bool IsFire;
    public float2 Movement;
}
