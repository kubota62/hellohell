/// <summary>
/// 攻撃マスタを参照するための軽量ID。
/// 将来は ScriptableObject や BlobAsset の定義IDと対応させる。
/// </summary>
public enum AttackDefinitionId
{
    BasicProjectile = 1,
}

/// <summary>
/// 攻撃マスタから取得する Projectile 攻撃の調整値。
/// いまは仮実装として静的カタログまたは Baker が作る定義バッファから返す。
/// </summary>
public struct ProjectileAttackDefinition
{
    public AttackDefinitionId Id;
    public float Speed;
    public int Damage;
    public float HitRadius;
    public float Lifetime;
    public float Scale;
}
