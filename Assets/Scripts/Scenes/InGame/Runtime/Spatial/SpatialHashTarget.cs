using Unity.Entities;

/// <summary>
/// Broadphase クエリ用の空間ハッシュへ挿入するエンティティを示す。
/// Team、Hitbox、LocalTransform と組み合わせて、命中判定用のグリッドを作る。
/// </summary>
public struct SpatialHashTarget : IComponentData
{
}
