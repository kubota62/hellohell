using Unity.Entities;

/// <summary>
/// 空間ハッシュベースの戦闘クエリで使う単純な球形ヒット範囲。
/// Radius はエンティティの LocalTransform 位置を中心としたワールド単位で扱う。
/// </summary>
public struct Hitbox : IComponentData
{
    public float Radius;
}
