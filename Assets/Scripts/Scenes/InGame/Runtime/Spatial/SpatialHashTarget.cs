using Unity.Entities;

/// <summary>
/// Broadphase クエリ用の空間ハッシュへ挿入するエンティティを示す。
/// このマーカーと Team、Hitbox、LocalTransform を読むことで、ハッシュ構築を汎用化できる。
/// </summary>
public struct SpatialHashTarget : IComponentData
{
}
