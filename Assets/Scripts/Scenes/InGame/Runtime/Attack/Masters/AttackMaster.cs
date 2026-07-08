/// <summary>
/// 攻撃マスタを参照するための軽量ID。
/// 将来的には ScriptableObject や BlobAsset の定義IDと対応させる。
/// </summary>
public enum AttackMasterId
{
    BasicProjectile = 1,
    BasicAura = 2,
    BasicMeleeArc = 3,
    BasicChainProjectile = 4,
}

/// <summary>
/// 攻撃の実行方式。
/// Projectile、Aura、MeleeArc、Beam などを同じマスタ窓口から追加できるようにする。
/// </summary>
public enum AttackKind : byte
{
    Projectile = 1,
    Aura = 2,
    MeleeArc = 3,
    Beam = 4,
}

/// <summary>
/// Projectile 攻撃に合成できる追加性質。
/// 複数の性質を組み合わせられるよう、単一 enum 分岐ではなくビットフラグで扱う。
/// </summary>
public enum ProjectileModifierFlags : byte
{
    None = 0,
    Piercing = 1 << 0,
    Chaining = 1 << 1,
    AreaOfEffect = 1 << 2,
}

/// <summary>
/// 攻撃マスタから取得するランタイム用の調整値。
/// 共通値と攻撃方式ごとの固有値を同じ構造体に置き、必要に応じて段階的に分割する。
/// </summary>
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
