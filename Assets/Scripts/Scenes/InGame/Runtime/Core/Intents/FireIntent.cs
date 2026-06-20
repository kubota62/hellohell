using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 入力と AI が共有する、構造変更を伴わない射撃意図。
/// 大量エンティティで毎フレーム Request タグを追加削除するコストを避ける。
/// </summary>
public struct FireIntent : IComponentData
{
    public bool IsPressed;
    public float3 Origin;
    public float3 Direction;
}
