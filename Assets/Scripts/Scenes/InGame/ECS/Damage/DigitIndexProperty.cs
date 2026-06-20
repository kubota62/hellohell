using Unity.Entities;

[Unity.Rendering.MaterialProperty("_DigitIndex")]
public struct DigitIndexProperty : IComponentData
{
    public float Value;
}
