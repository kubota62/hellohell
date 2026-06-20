using Unity.Entities;
using Unity.Mathematics;

public struct DamageDigit : IComponentData
{
    public float3 StartPosition;
    public float HorizontalOffset;
    public float Elapsed;
    public float Lifetime;
}
