using Unity.Entities;

/// <summary>
/// 攻撃マスターの検索と、アセット未登録時の標準値を提供する。
/// </summary>
public static class AttackMasterCatalog
{
    public static AttackMasterData Get(AttackMasterId id)
    {
        switch (id)
        {
            case AttackMasterId.BasicAura:
                return CreateArea(
                    id,
                    AttackKind.Aura,
                    cooldown: 1.5f,
                    damage: 20,
                    radius: 3f,
                    angleDegrees: 360f,
                    visualDuration: 0.12f);

            case AttackMasterId.BasicMeleeArc:
                return CreateArea(
                    id,
                    AttackKind.MeleeArc,
                    cooldown: 0.45f,
                    damage: 28,
                    radius: 5.5f,
                    angleDegrees: 100f,
                    visualDuration: 0.14f);

            case AttackMasterId.BasicChainProjectile:
                return CreateProjectile(
                    id,
                    cooldown: 0.9f,
                    damage: 22,
                    hitRadius: 0.5f,
                    speed: 12f,
                    lifetime: 4f,
                    scale: 0.45f,
                    modifiers: ProjectileModifierFlags.Chaining,
                    chainCount: 3,
                    chainRange: 7f);

            case AttackMasterId.RapidBolt:
                return CreateProjectile(
                    id,
                    cooldown: 0.38f,
                    damage: 22,
                    hitRadius: 0.28f,
                    speed: 20f,
                    lifetime: 3.5f,
                    scale: 0.32f);

            case AttackMasterId.PiercingLance:
                return CreateProjectile(
                    id,
                    cooldown: 0.72f,
                    damage: 32,
                    hitRadius: 0.3f,
                    speed: 19f,
                    lifetime: 4f,
                    scale: 0.34f,
                    modifiers: ProjectileModifierFlags.Piercing,
                    pierceCount: 6);

            case AttackMasterId.ExplosiveOrb:
                return CreateProjectile(
                    id,
                    cooldown: 1.8f,
                    damage: 58,
                    hitRadius: 0.65f,
                    speed: 6.5f,
                    lifetime: 5.5f,
                    scale: 0.72f,
                    modifiers: ProjectileModifierFlags.AreaOfEffect,
                    impactAreaRadius: 3.2f);

            case AttackMasterId.BasicProjectile:
            default:
                return CreateProjectile(
                    AttackMasterId.BasicProjectile,
                    cooldown: 1f,
                    damage: 34,
                    hitRadius: 0.5f,
                    speed: 10f,
                    lifetime: 5f,
                    scale: 0.5f);
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

    private static AttackMasterData CreateProjectile(
        AttackMasterId id,
        float cooldown,
        int damage,
        float hitRadius,
        float speed,
        float lifetime,
        float scale,
        ProjectileModifierFlags modifiers = ProjectileModifierFlags.None,
        int pierceCount = 0,
        int chainCount = 0,
        float chainRange = 0f,
        float impactAreaRadius = 0f)
    {
        return new AttackMasterData
        {
            Id = id,
            Kind = AttackKind.Projectile,
            Cooldown = cooldown,
            Damage = damage,
            HitRadius = hitRadius,
            ProjectileSpeed = speed,
            Lifetime = lifetime,
            Scale = scale,
            ProjectileModifiers = modifiers,
            PierceCount = pierceCount,
            ChainCount = chainCount,
            ChainRange = chainRange,
            ImpactAreaRadius = impactAreaRadius,
        };
    }

    private static AttackMasterData CreateArea(
        AttackMasterId id,
        AttackKind kind,
        float cooldown,
        int damage,
        float radius,
        float angleDegrees,
        float visualDuration)
    {
        return new AttackMasterData
        {
            Id = id,
            Kind = kind,
            Cooldown = cooldown,
            Damage = damage,
            AreaRadius = radius,
            Scale = 1f,
            ArcAngleDegrees = angleDegrees,
            VisualDuration = visualDuration,
        };
    }
}
