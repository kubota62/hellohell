using Unity.Entities;

/// <summary>
/// 攻撃マスタへアクセスする仮の窓口。
/// Baker が作った定義バッファがある場合はそちらを優先し、未配置でも静的な既定値で動く。
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

    public static ProjectileAttackDefinition GetProjectile(
        DynamicBuffer<AttackDefinitionElement> definitions,
        AttackDefinitionId id)
    {
        for (var i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].Id == id)
            {
                return definitions[i].ToProjectileDefinition();
            }
        }

        return GetProjectile(id);
    }
}
