using Unity.Entities;

/// <summary>
/// Baker が ScriptableObject の攻撃定義を ECS 側へ渡すためのバッファ要素。
/// 実行中のシステムは managed な ScriptableObject ではなく、この値だけを読む。
/// </summary>
public struct AttackMasterElement : IBufferElementData
{
    public AttackMasterId Id;
    public AttackKind Kind;
    public float Cooldown;
    public int Damage;
    public float HitRadius;
    public float AreaRadius;
    public float ProjectileSpeed;
    public float Lifetime;
    public float Scale;
    public ProjectileModifierFlags ProjectileModifiers;
    public int PierceCount;
    public int ChainCount;
    public float ChainRange;
    public float ImpactAreaRadius;
    public float ArcAngleDegrees;
    public float VisualDuration;

    public static AttackMasterElement FromMaster(AttackMasterData definition)
    {
        return new AttackMasterElement
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
            ProjectileModifiers = definition.ProjectileModifiers,
            PierceCount = definition.PierceCount,
            ChainCount = definition.ChainCount,
            ChainRange = definition.ChainRange,
            ImpactAreaRadius = definition.ImpactAreaRadius,
            ArcAngleDegrees = definition.ArcAngleDegrees,
            VisualDuration = definition.VisualDuration,
        };
    }

    public AttackMasterData ToMaster()
    {
        return new AttackMasterData
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
            ProjectileModifiers = ProjectileModifiers,
            PierceCount = PierceCount,
            ChainCount = ChainCount,
            ChainRange = ChainRange,
            ImpactAreaRadius = ImpactAreaRadius,
            ArcAngleDegrees = ArcAngleDegrees,
            VisualDuration = VisualDuration,
        };
    }
}
