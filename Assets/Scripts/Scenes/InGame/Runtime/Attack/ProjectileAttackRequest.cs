using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Projectile 攻撃を生成するための一回限りのリクエスト。
/// 発射判断側が攻撃マスタを解決して作り、ProjectileSpawnSystem が消費して破棄する。
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
}
