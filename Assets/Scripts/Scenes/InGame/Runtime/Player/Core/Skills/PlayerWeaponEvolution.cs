using Unity.Mathematics;

/// <summary>最大レベル武器へ固有の挙動変化を与える。</summary>
public static class PlayerWeaponEvolutionUtility
{
    public const int EvolutionLevel = 8;

    public static bool IsEvolved(
        in PlayerSkillStats stats,
        AttackMasterId attackMasterId)
    {
        return PlayerAutoSkillSystem.GetWeaponLevel(
            stats,
            attackMasterId) >= EvolutionLevel;
    }

    public static void Apply(
        ref AttackMasterData definition,
        in PlayerSkillStats stats)
    {
        if (!IsEvolved(stats, definition.Id))
        {
            return;
        }

        switch (definition.Id)
        {
            case AttackMasterId.BasicMeleeArc:
                ApplyMeleeArc(ref definition);
                break;

            case AttackMasterId.RapidBolt:
                ApplyRapidBolt(ref definition);
                break;

            case AttackMasterId.PiercingLance:
                ApplyPiercingLance(ref definition);
                break;

            case AttackMasterId.ExplosiveOrb:
                ApplyExplosiveOrb(ref definition);
                break;
        }
    }

    public static string GetBaseName(AttackMasterId attackMasterId)
    {
        switch (attackMasterId)
        {
            case AttackMasterId.BasicMeleeArc:
                return "斬撃";
            case AttackMasterId.RapidBolt:
                return "速射";
            case AttackMasterId.PiercingLance:
                return "貫通槍";
            case AttackMasterId.ExplosiveOrb:
                return "爆裂球";
            default:
                return "武器";
        }
    }

    public static string GetEvolutionName(AttackMasterId attackMasterId)
    {
        switch (attackMasterId)
        {
            case AttackMasterId.BasicMeleeArc:
                return "血月斬";
            case AttackMasterId.RapidBolt:
                return "雷雨弾";
            case AttackMasterId.PiercingLance:
                return "破城槍";
            case AttackMasterId.ExplosiveOrb:
                return "煉獄球";
            default:
                return string.Empty;
        }
    }

    public static string GetEvolutionName(PlayerSkillKind kind)
    {
        switch (kind)
        {
            case PlayerSkillKind.MeleeArc:
                return GetEvolutionName(AttackMasterId.BasicMeleeArc);
            case PlayerSkillKind.RapidBolt:
                return GetEvolutionName(AttackMasterId.RapidBolt);
            case PlayerSkillKind.PiercingLance:
                return GetEvolutionName(AttackMasterId.PiercingLance);
            case PlayerSkillKind.ExplosiveOrb:
                return GetEvolutionName(AttackMasterId.ExplosiveOrb);
            default:
                return string.Empty;
        }
    }

    static void ApplyMeleeArc(ref AttackMasterData definition)
    {
        definition.Damage = ScaleDamage(definition.Damage, 1.3f);
        definition.Cooldown = math.max(0.05f, definition.Cooldown * 0.82f);
        definition.AreaRadius *= 1.35f;
        definition.Scale *= 1.35f;
        definition.ArcAngleDegrees = math.max(
            160f,
            definition.ArcAngleDegrees);
        definition.VisualDuration *= 1.25f;
    }

    static void ApplyRapidBolt(ref AttackMasterData definition)
    {
        definition.Damage = ScaleDamage(definition.Damage, 1.2f);
        definition.Cooldown = math.max(0.05f, definition.Cooldown * 0.65f);
        definition.ProjectileSpeed *= 1.35f;
        definition.HitRadius *= 1.35f;
        definition.Scale *= 1.25f;
        definition.PierceCount = math.max(0, definition.PierceCount) + 1;
        definition.ProjectileModifiers |= ProjectileModifierFlags.Piercing;
    }

    static void ApplyPiercingLance(ref AttackMasterData definition)
    {
        definition.Damage = ScaleDamage(definition.Damage, 1.25f);
        definition.PierceCount = math.max(0, definition.PierceCount) + 8;
        definition.ProjectileModifiers |=
            ProjectileModifierFlags.Piercing |
            ProjectileModifierFlags.AreaOfEffect;
        definition.ImpactAreaRadius = math.max(
            definition.ImpactAreaRadius,
            math.max(1.6f, definition.HitRadius * 5.3f));
        definition.Scale *= 1.3f;
    }

    static void ApplyExplosiveOrb(ref AttackMasterData definition)
    {
        definition.Damage = ScaleDamage(definition.Damage, 1.3f);
        definition.Cooldown = math.max(0.05f, definition.Cooldown * 0.82f);
        definition.ProjectileSpeed *= 1.15f;
        definition.ImpactAreaRadius *= 1.65f;
        definition.Scale *= 1.25f;
        definition.ChainCount = math.max(2, definition.ChainCount);
        definition.ChainRange = math.max(6f, definition.ChainRange);
        definition.ProjectileModifiers |=
            ProjectileModifierFlags.AreaOfEffect |
            ProjectileModifierFlags.Chaining;
    }

    static int ScaleDamage(int damage, float multiplier)
    {
        return math.max(
            1,
            (int)math.round(math.max(1, damage) * multiplier));
    }
}
