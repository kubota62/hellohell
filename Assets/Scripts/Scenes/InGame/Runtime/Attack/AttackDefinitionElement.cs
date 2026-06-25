using Unity.Entities;

/// <summary>
/// Baker が ScriptableObject の攻撃定義を ECS 側へ渡すためのバッファ要素。
/// 実行中のシステムは managed な ScriptableObject ではなく、この値だけを読む。
/// </summary>
public struct AttackDefinitionElement : IBufferElementData
{
    public AttackDefinitionId Id;
    public AttackKind Kind;
    public float Cooldown;
    public int Damage;
    public float HitRadius;
    public float AreaRadius;
    public float ProjectileSpeed;
    public float Lifetime;
    public float Scale;

    public static AttackDefinitionElement FromDefinition(AttackDefinitionData definition)
    {
        return new AttackDefinitionElement
        {
            Id = definition.Id,
            Kind = definition.Kind,
            Cooldown = definition.Cooldown,
            Damage = definition.Damage,
            HitRadius = definition.HitRadius,
            AreaRadius = definition.AreaRadius,
            ProjectileSpeed = definition.ProjectileSpeed,
            Lifetime = definition.Lifetime,
            Scale = definition.Scale,
        };
    }

    public AttackDefinitionData ToDefinition()
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
