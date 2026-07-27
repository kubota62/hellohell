using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// ダメージ数値表示の開始位置と寿命を持つ演出用コンポーネント。
/// </summary>
public struct DamageDigit : IComponentData
{
    public float3 StartPosition;
    public float HorizontalOffset;
    public float Elapsed;
    public float Lifetime;
    public byte IsCritical;
    public byte IsPlayerDamage;
}
