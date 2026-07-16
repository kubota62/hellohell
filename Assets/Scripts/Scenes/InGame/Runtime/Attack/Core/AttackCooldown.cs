using Unity.Entities;

/// <summary>
/// Attack/Core の共通コンポーネント。
/// Actorごとの攻撃クールダウンを持ち、攻撃マスタの Cooldown を反映して発射間隔を制御する。
/// </summary>
public struct AttackCooldown : IComponentData
{
    public float Remaining;
}

/// <summary>
/// Playerが複数攻撃を同時に扱うための、攻撃マスタごとの独立クールダウン。
/// </summary>
public struct PlayerAttackCooldown : IBufferElementData
{
    public AttackMasterId AttackMasterId;
    public float Remaining;
}
