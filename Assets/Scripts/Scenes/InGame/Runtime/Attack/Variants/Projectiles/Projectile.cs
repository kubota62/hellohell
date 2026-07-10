using Unity.Entities;

/// <summary>
/// Attack/Variants/Projectiles の命中フィルタとダメージ解決に使う共通データ。
/// 移動状態は ProjectileMotion に分け、複数のProjectile挙動で共有できるようにする。
/// </summary>
public struct Projectile : IComponentData
{
    public Entity Owner;
    public TeamId Team;
    public int Damage;
    public float HitRadius;
}

/// <summary>
/// Projectile の追加性質を保持するランタイム状態。
/// 貫通、チェーン、範囲などのタグと併用し、命中処理が必要な数値だけを読む。
/// </summary>
public struct ProjectileModifierState : IComponentData
{
    public ProjectileModifierFlags Modifiers;
    public int PierceRemaining;
    public int ChainRemaining;
    public float ChainRange;
    public float ImpactAreaRadius;
}

/// <summary>
/// 貫通弾やチェーン弾が同じ対象へ連続ヒットしないよう、命中済み対象を記録するバッファ。
/// </summary>
public struct ProjectileHitRecord : IBufferElementData
{
    public Entity Target;
}

/// <summary>
/// 一定回数、命中しても消えずに貫通するProjectileを示すタグ。
/// </summary>
public struct PiercingProjectile : IComponentData
{
}

/// <summary>
/// 命中後に近くの敵へ軌道をつなぐProjectileを示すタグ。
/// </summary>
public struct ChainingProjectile : IComponentData
{
}

/// <summary>
/// 命中地点の周囲へ追加ダメージを与えるProjectileを示すタグ。
/// </summary>
public struct AreaOfEffectProjectile : IComponentData
{
}
