using Unity.Entities;

/// <summary>
/// Baker が ScriptableObject の攻撃定義を ECS 側へ渡すためのバッファ要素。
/// 実行中のシステムは managed な ScriptableObject ではなく、この値だけを読む。
/// </summary>
public struct AttackDefinitionElement : IBufferElementData
{
    public AttackDefinitionId Id;
    public float Speed;
    public int Damage;
    public float HitRadius;
    public float Lifetime;
    public float Scale;

    public static AttackDefinitionElement FromDefinition(ProjectileAttackDefinition definition)
    {
        return new AttackDefinitionElement
        {
            Id = definition.Id,
            Speed = definition.Speed,
            Damage = definition.Damage,
            HitRadius = definition.HitRadius,
            Lifetime = definition.Lifetime,
            Scale = definition.Scale,
        };
    }

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
