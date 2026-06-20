using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Projectile 型攻撃の移動状態。
/// Projectile は戦闘メタデータを持ち、このコンポーネントは移動データだけを持つ。
/// </summary>
public struct ProjectileMotion : IComponentData
{
    public Entity Shooter;
    public float3 Velocity;
}
