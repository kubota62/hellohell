using UnityEngine;

/// <summary>
/// 攻撃の調整値をエディタ上で管理する ScriptableObject。
/// 現段階では Projectile 攻撃だけを扱い、後で Baker や BlobAsset へ接続する。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Definitions/Attack Definition")]
public class AttackDefinitionAsset : ScriptableObject
{
    public AttackDefinitionId Id = AttackDefinitionId.BasicProjectile;
    public float Speed = 10f;
    public int Damage = 34;
    public float HitRadius = 0.5f;
    public float Lifetime = 5f;
    public float Scale = 0.5f;

    public ProjectileAttackDefinition ToProjectileDefinition()
    {
        return new ProjectileAttackDefinition
        {
            Id = Id,
            Speed = Speed,
            Damage = Damage,
            HitRadius = HitRadius,
            Lifetime = Lifetime,
            Scale = Scale,
        };
    }
}
