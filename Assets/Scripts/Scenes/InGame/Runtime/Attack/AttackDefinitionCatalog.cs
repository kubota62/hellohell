/// <summary>
/// 攻撃マスタへアクセスする仮の窓口。
/// 後で ScriptableObject Authoring や Baker に差し替えても、呼び出し側の形を保てるようにする。
/// </summary>
public static class AttackDefinitionCatalog
{
    public static ProjectileAttackDefinition GetProjectile(AttackDefinitionId id)
    {
        switch (id)
        {
            case AttackDefinitionId.BasicProjectile:
            default:
                return new ProjectileAttackDefinition
                {
                    Id = AttackDefinitionId.BasicProjectile,
                    Speed = 10f,
                    Damage = 34,
                    HitRadius = 0.5f,
                    Lifetime = 5f,
                    Scale = 0.5f,
                };
        }
    }
}
