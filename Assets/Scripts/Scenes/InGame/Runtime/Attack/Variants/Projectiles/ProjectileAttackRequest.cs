using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Attack/Variants/Projectiles 用の一回きりの生成リクエスト。
/// AttackRequestSystem が攻撃マスタから作り、ProjectileSpawnSystem が消費して弾を実体化する。
/// </summary>
public struct ProjectileAttackRequest : IComponentData
{
    public AttackMasterId AttackMasterId;
    public Entity Owner;
    public TeamId Team;
    public float3 Position;
    public float3 Direction;
    public float Speed;
    public int Damage;
    public byte IsCritical;
    public float HitRadius;
    public float Lifetime;
    public float Scale;
    public ProjectileModifierFlags Modifiers;
    public int PierceCount;
    public int ChainCount;
    public float ChainRange;
    public float ImpactAreaRadius;
}
