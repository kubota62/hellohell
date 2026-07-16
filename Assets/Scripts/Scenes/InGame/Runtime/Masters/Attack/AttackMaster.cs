/// <summary>攻撃マスターを識別するID。</summary>
public enum AttackMasterId
{
    None = 0,
    BasicProjectile = 1,
    BasicAura = 2,
    BasicMeleeArc = 3,
    BasicChainProjectile = 4,
    RapidBolt = 5,
    PiercingLance = 6,
    ExplosiveOrb = 7,
}

/// <summary>攻撃の実行方式。</summary>
public enum AttackKind : byte
{
    Projectile = 1,
    Aura = 2,
    MeleeArc = 3,
    Beam = 4,
}

/// <summary>Projectileへ追加する挙動。複数指定できる。</summary>
public enum ProjectileModifierFlags : byte
{
    None = 0,
    Piercing = 1 << 0,
    Chaining = 1 << 1,
    AreaOfEffect = 1 << 2,
}

/// <summary>攻撃IDの共通一覧とビットマスク操作。</summary>
public static class AttackMasterIdUtility
{
    public static readonly AttackMasterId[] All =
    {
        AttackMasterId.BasicProjectile,
        AttackMasterId.BasicAura,
        AttackMasterId.BasicMeleeArc,
        AttackMasterId.BasicChainProjectile,
        AttackMasterId.RapidBolt,
        AttackMasterId.PiercingLance,
        AttackMasterId.ExplosiveOrb,
    };

    public static readonly AttackMasterId[] PlayerDefaults =
    {
        AttackMasterId.BasicMeleeArc,
        AttackMasterId.RapidBolt,
        AttackMasterId.PiercingLance,
        AttackMasterId.ExplosiveOrb,
    };

    public static uint ToMask(this AttackMasterId id)
    {
        return id == AttackMasterId.None ? 0u : 1u << (int)id;
    }

    public static bool Contains(this uint mask, AttackMasterId id)
    {
        return (mask & id.ToMask()) != 0u;
    }

    public static uint CreatePlayerDefaultMask()
    {
        var mask = 0u;
        for (var i = 0; i < PlayerDefaults.Length; i++)
        {
            mask |= PlayerDefaults[i].ToMask();
        }

        return mask;
    }
}

public static class ProjectileModifierFlagsUtility
{
    public static bool Has(
        this ProjectileModifierFlags value,
        ProjectileModifierFlags flag)
    {
        return (value & flag) != 0;
    }
}

/// <summary>実行時に参照する攻撃設定。</summary>
public struct AttackMasterData
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
}
