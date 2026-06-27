using Unity.Entities;

/// <summary>
/// 破棄せず再利用できるエンティティであることと、生成元プレハブを記録する。
/// 再利用時は構造変更ではなく GameplayActive の有効/無効でゲーム参加状態を切り替える。
/// </summary>
public struct PooledInstance : IComponentData
{
    public Entity SourcePrefab;
}
