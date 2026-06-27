using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Projectile 型攻撃の移動状態。
/// 戦闘メタデータとは分け、移動システムが必要な速度と発射者だけを持つ。
/// </summary>
public struct ProjectileMotion : IComponentData
{
    public Entity Shooter;
    public float3 Velocity;
}
