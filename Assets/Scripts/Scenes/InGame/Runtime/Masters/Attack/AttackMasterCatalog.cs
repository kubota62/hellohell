using Unity.Entities;

/// <summary>
/// 攻撃マスタへアクセスする仮の窓口。
/// Baker が作った定義バッファがあればそちらを優先し、未配置でも最低限の既定値で動く。
/// </summary>
public static class AttackMasterCatalog
{
    public static AttackMasterData Get(AttackMasterId id)
    {
        switch (id)
        {
            case AttackMasterId.ExplosiveOrb:
                return new AttackMasterData
                {
                    Id = AttackMasterId.ExplosiveOrb,
                    Kind = AttackKind.Projectile,
                    Cooldown = 1.8f,
                    Damage = 58,
                    HitRadius = 0.65f,
                    AreaRadius = 0f,
                    ProjectileSpeed = 6.5f,
                    Lifetime = 5.5f,
                    Scale = 0.72f,
                    ProjectileModifiers = ProjectileModifierFlags.AreaOfEffect,
                    PierceCount = 0,
                    ChainCount = 0,
                    ChainRange = 0f,
                    ImpactAreaRadius = 3.2f,
                    ArcAngleDegrees = 0f,
                    VisualDuration = 0f,
                };

            case AttackMasterId.PiercingLance:
                return new AttackMasterData
                {
                    Id = AttackMasterId.PiercingLance,
                    Kind = AttackKind.Projectile,
                    Cooldown = 0.72f,
                    Damage = 32,
                    HitRadius = 0.3f,
                    AreaRadius = 0f,
                    ProjectileSpeed = 19f,
                    Lifetime = 4f,
                    Scale = 0.34f,
                    ProjectileModifiers = ProjectileModifierFlags.Piercing,
                    PierceCount = 6,
                    ChainCount = 0,
                    ChainRange = 0f,
                    ImpactAreaRadius = 0f,
                    ArcAngleDegrees = 0f,
                    VisualDuration = 0f,
                };

            case AttackMasterId.RapidBolt:
                return new AttackMasterData
                {
                    Id = AttackMasterId.RapidBolt,
                    Kind = AttackKind.Projectile,
                    Cooldown = 0.38f,
                    Damage = 22,
                    HitRadius = 0.28f,
                    AreaRadius = 0f,
                    ProjectileSpeed = 20f,
                    Lifetime = 3.5f,
                    Scale = 0.32f,
                    ProjectileModifiers = ProjectileModifierFlags.None,
                    PierceCount = 0,
                    ChainCount = 0,
                    ChainRange = 0f,
                    ImpactAreaRadius = 0f,
                    ArcAngleDegrees = 0f,
                    VisualDuration = 0f,
                };

            case AttackMasterId.BasicAura:
                return new AttackMasterData
                {
                    Id = AttackMasterId.BasicAura,
                    Kind = AttackKind.Aura,
                    Cooldown = 1.5f,
                    Damage = 20,
                    HitRadius = 0f,
                    AreaRadius = 3f,
                    ProjectileSpeed = 0f,
                    Lifetime = 0f,
                    Scale = 1f,
                    ProjectileModifiers = ProjectileModifierFlags.None,
                    PierceCount = 0,
                    ChainCount = 0,
                    ChainRange = 0f,
                    ImpactAreaRadius = 0f,
                    ArcAngleDegrees = 360f,
                    VisualDuration = 0.12f,
                };

            case AttackMasterId.BasicMeleeArc:
                return new AttackMasterData
                {
                    Id = AttackMasterId.BasicMeleeArc,
                    Kind = AttackKind.MeleeArc,
                    Cooldown = 0.45f,
                    Damage = 28,
                    HitRadius = 0f,
                    AreaRadius = 5.5f,
                    ProjectileSpeed = 0f,
                    Lifetime = 0f,
                    Scale = 1f,
                    ProjectileModifiers = ProjectileModifierFlags.None,
                    PierceCount = 0,
                    ChainCount = 0,
                    ChainRange = 0f,
                    ImpactAreaRadius = 0f,
                    ArcAngleDegrees = 100f,
                    VisualDuration = 0.14f,
                };

            case AttackMasterId.BasicChainProjectile:
                return new AttackMasterData
                {
                    Id = AttackMasterId.BasicChainProjectile,
                    Kind = AttackKind.Projectile,
                    Cooldown = 0.9f,
                    Damage = 22,
                    HitRadius = 0.5f,
                    AreaRadius = 0f,
                    ProjectileSpeed = 12f,
                    Lifetime = 4f,
                    Scale = 0.45f,
                    ProjectileModifiers = ProjectileModifierFlags.Chaining,
                    PierceCount = 0,
                    ChainCount = 3,
                    ChainRange = 7f,
                    ImpactAreaRadius = 0f,
                    ArcAngleDegrees = 0f,
                    VisualDuration = 0f,
                };

            case AttackMasterId.BasicProjectile:
            default:
                return new AttackMasterData
                {
                    Id = AttackMasterId.BasicProjectile,
                    Kind = AttackKind.Projectile,
                    Cooldown = 1f,
                    Damage = 34,
                    HitRadius = 0.5f,
                    AreaRadius = 0f,
                    ProjectileSpeed = 10f,
                    Lifetime = 5f,
                    Scale = 0.5f,
                    ProjectileModifiers = ProjectileModifierFlags.None,
                    PierceCount = 0,
                    ChainCount = 0,
                    ChainRange = 0f,
                    ImpactAreaRadius = 0f,
                    ArcAngleDegrees = 0f,
                    VisualDuration = 0f,
                };
        }
    }

    public static AttackMasterData Get(
        DynamicBuffer<AttackMasterElement> definitions,
        AttackMasterId id)
    {
        for (var i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].Id == id)
            {
                return definitions[i].ToMaster();
            }
        }

        return Get(id);
    }
}
