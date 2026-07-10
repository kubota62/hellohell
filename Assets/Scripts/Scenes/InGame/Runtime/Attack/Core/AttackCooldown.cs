using Unity.Entities;

/// <summary>
/// Attack/Core の共通コンポーネント。
/// Actorごとの攻撃クールダウンを持ち、攻撃マスタの Cooldown を反映して発射間隔を制御する。
/// </summary>
public struct AttackCooldown : IComponentData
{
    public float Remaining;
}
