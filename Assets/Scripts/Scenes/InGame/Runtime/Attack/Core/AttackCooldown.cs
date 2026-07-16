using Unity.Entities;

/// <summary>単一攻撃を持つActor用のクールダウン。</summary>
public struct AttackCooldown : IComponentData
{
    public float Remaining;
}

/// <summary>Playerが装備する攻撃と、その独立クールダウン。</summary>
public struct PlayerAttackSlot : IBufferElementData
{
    public AttackMasterId AttackMasterId;
    public float Remaining;
}
