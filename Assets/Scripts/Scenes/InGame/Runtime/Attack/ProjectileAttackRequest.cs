using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Projectile攻撃を生成するための一回きりのリクエスト。
/// AttackRequestSystem が攻撃マスタから作り、ProjectileSpawnSystem が消費して実体化する。
/// </summary>
public struct ProjectileAttackRequest : IComponentData
{
    public AttackDefinitionId AttackDefinitionId;
    public Entity Owner;
    public TeamId Team;
    public float3 Position;
    public float3 Direction;
    public float Speed;
    public int Damage;
    public float HitRadius;
    public float Lifetime;
    public float Scale;
    public ProjectileModifierFlags Modifiers;
    public int PierceCount;
    public int ChainCount;
    public float ChainRange;
    public float ImpactAreaRadius;
}
