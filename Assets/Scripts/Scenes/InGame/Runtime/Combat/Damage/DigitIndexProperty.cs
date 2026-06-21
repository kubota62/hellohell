using Unity.Entities;

/// <summary>
/// ダメージ数字マテリアルに、表示する数字のインデックスを渡すプロパティ。
/// </summary>
[Unity.Rendering.MaterialProperty("_DigitIndex")]
public struct DigitIndexProperty : IComponentData
{
    public float Value;
}
