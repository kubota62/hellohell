using Unity.Entities;

/// <summary>
/// グリッドベースの broadphase に使うシングルトン設定。
/// CellSize は近傍チェックを減らすため、よく使う最大ヒット半径以上を目安にする。
/// </summary>
public struct SpatialHashSettings : IComponentData
{
    public float CellSize;
}
