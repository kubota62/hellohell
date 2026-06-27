using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Player 入力や Enemy AI が共有する、構造変更を伴わない移動意図。
/// 生成側が値を更新し、移動適用システムが実際の座標と向きへ反映する。
/// </summary>
public struct MoveIntent : IComponentData
{
    public float3 Direction;
    public float Magnitude;
}
