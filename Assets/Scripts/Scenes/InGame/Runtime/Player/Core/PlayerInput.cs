using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Player/Core の共通入力状態。
/// PlayerInputManager が更新し、ECS 側の Player 移動と射撃システムが参照する入力状態。
/// Movement は XZ 平面上の移動方向として扱う。
/// </summary>
public struct PlayerInput : IComponentData
{
    public bool IsFire;
    public bool HasAimPosition;
    public bool AutoAttackEnabled;
    public float2 Movement;
    public float3 AimWorldPosition;
}
