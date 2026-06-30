using Unity.Entities;

/// <summary>
/// 命中フィルタとダメージ解決に使う Projectile 共通メタデータ。
/// 移動状態は ProjectileMotion に分け、複数の Projectile 移動モデルを共存させる。
/// </summary>
public struct Projectile : IComponentData
{
    public Entity Owner;
    public TeamId Team;
    public int Damage;
    public float HitRadius;
}

/// <summary>
/// Projectile の追加性質を保持する共通状態。
/// タグコンポーネントと併用し、命中システムが必要な性質だけを読めるようにする。
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
/// 貫通弾や範囲弾が同じ対象へ連続ヒットしないよう、命中済み対象を記録するバッファ。
/// </summary>
public struct ProjectileHitRecord : IBufferElementData
{
    public Entity Target;
}

/// <summary>
/// 命中しても一定回数消えずに貫通する Projectile。
/// </summary>
public struct PiercingProjectile : IComponentData
{
}

/// <summary>
/// 命中後に近くの敵へ連鎖する Projectile。
/// </summary>
public struct ChainingProjectile : IComponentData
{
}

/// <summary>
/// 命中地点の周囲へ追加効果を与える Projectile。
/// </summary>
public struct AreaOfEffectProjectile : IComponentData
{
}
