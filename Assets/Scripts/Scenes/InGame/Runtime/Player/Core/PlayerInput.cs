using Unity.Entities;
using Unity.Mathematics;

/// <summary>Managed入力をECSへ渡すSingletonコンポーネント。</summary>
public struct PlayerInput : IComponentData
{
    public bool IsFire;
    public bool HasAimPosition;
    public bool AutoAttackEnabled;
    public float2 Movement;
    public float3 AimWorldPosition;
    public uint ActiveAttackMask;
}
