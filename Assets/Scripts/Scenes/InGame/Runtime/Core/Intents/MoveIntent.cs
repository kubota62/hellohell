using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Player 入力と Enemy AI が共有する、構造変更を伴わない移動意図。
/// 生成側が値を更新し、移動システムがそれを適用する。
/// </summary>
public struct MoveIntent : IComponentData
{
    public float3 Direction;
    public float Magnitude;
}
