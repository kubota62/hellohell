/// <summary>
/// 攻撃マスタを参照するための軽量ID。
/// 将来的には ScriptableObject や BlobAsset の定義IDと対応させる。
/// </summary>
public enum AttackDefinitionId
{
    BasicProjectile = 1,
    BasicAura = 2,
}

/// <summary>
/// 攻撃の実行方法。
/// いまは Projectile のみ実装し、他の種類は同じマスタ窓口から追加できるように予約しておく。
/// </summary>
public enum AttackKind : byte
{
    Projectile = 1,
    Aura = 2,
    MeleeArc = 3,
    Beam = 4,
}

/// <summary>
/// 攻撃マスタから取得するランタイム用の調整値。
/// 共通値と Projectile 固有値を同じ構造体に置き、攻撃手段が増えたら必要な項目を段階的に分離する。
/// </summary>
public struct AttackDefinitionData
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
}
