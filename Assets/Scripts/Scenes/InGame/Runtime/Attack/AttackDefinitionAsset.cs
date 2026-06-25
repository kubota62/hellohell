using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 攻撃の調整値をエディタ上で管理する ScriptableObject。
/// Projectile は攻撃手段の一種として扱い、将来的な Aura や Beam も同じ入口から追加する。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Definitions/Attack Definition")]
public class AttackDefinitionAsset : ScriptableObject
{
    public AttackDefinitionId Id = AttackDefinitionId.BasicProjectile;
    public AttackKind Kind = AttackKind.Projectile;
    public float Cooldown = 1f;
    public int Damage = 34;
    public float HitRadius = 0.5f;
    public float AreaRadius = 3f;
    [FormerlySerializedAs("Speed")]
    public float ProjectileSpeed = 10f;
    public float Lifetime = 5f;
    public float Scale = 0.5f;

    public AttackDefinitionData ToRuntimeDefinition()
    {
        return new AttackDefinitionData
        {
            Id = Id,
            Kind = Kind,
            Cooldown = Cooldown,
            Damage = Damage,
            HitRadius = HitRadius,
            AreaRadius = AreaRadius,
            ProjectileSpeed = ProjectileSpeed,
            Lifetime = Lifetime,
            Scale = Scale,
        };
    }
}
