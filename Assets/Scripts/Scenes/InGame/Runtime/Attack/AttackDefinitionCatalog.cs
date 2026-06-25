using Unity.Entities;

/// <summary>
/// 攻撃マスタへアクセスする仮の窓口。
/// Baker が作った定義バッファがあればそちらを優先し、未配置でも最低限の既定値で動く。
/// </summary>
public static class AttackDefinitionCatalog
{
    public static AttackDefinitionData Get(AttackDefinitionId id)
    {
        switch (id)
        {
            case AttackDefinitionId.BasicAura:
                return new AttackDefinitionData
                {
                    Id = AttackDefinitionId.BasicAura,
                    Kind = AttackKind.Aura,
                    Cooldown = 1.5f,
                    Damage = 20,
                    HitRadius = 0f,
                    AreaRadius = 3f,
                    ProjectileSpeed = 0f,
                    Lifetime = 0f,
                    Scale = 1f,
                };

            case AttackDefinitionId.BasicProjectile:
            default:
                return new AttackDefinitionData
                {
                    Id = AttackDefinitionId.BasicProjectile,
                    Kind = AttackKind.Projectile,
                    Cooldown = 1f,
                    Damage = 34,
                    HitRadius = 0.5f,
                    AreaRadius = 0f,
                    ProjectileSpeed = 10f,
                    Lifetime = 5f,
                    Scale = 0.5f,
                };
        }
    }

    public static AttackDefinitionData Get(
        DynamicBuffer<AttackDefinitionElement> definitions,
        AttackDefinitionId id)
    {
        for (var i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].Id == id)
            {
                return definitions[i].ToDefinition();
            }
        }

        return Get(id);
    }
}
