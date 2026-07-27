using Unity.Entities;

/// <summary>
/// 攻撃側から防御側へ渡すダメージ要求。
/// 複数の命中を同じフレームに受けても DynamicBuffer に積んでまとめて処理できる。
/// </summary>
public struct DamageEvent : IBufferElementData
{
    public int Damage;
    public Entity Attacker;
    public byte IsCritical;
}
