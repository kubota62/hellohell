using Unity.Entities;

/// <summary>
/// 命中フィルタとダメージ解決に使う Projectile 共通メタデータ。
/// 移動情報を分けることで、複数の Projectile 移動モデルを共存させる。
/// </summary>
public struct Projectile : IComponentData
{
    public Entity Owner;
    public TeamId Team;
    public int Damage;
    public float HitRadius;
}
