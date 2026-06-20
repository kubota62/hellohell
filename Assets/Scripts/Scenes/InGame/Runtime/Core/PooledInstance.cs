using Unity.Entities;

/// <summary>
/// 将来プール再利用できるエンティティであることと、生成元プレハブを記録する。
/// Step 0 では契約だけを定義し、生成負荷が見えてきた段階でプール本体を追加する。
/// </summary>
public struct PooledInstance : IComponentData
{
    public Entity SourcePrefab;
}
