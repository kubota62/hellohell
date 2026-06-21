using Unity.Entities;

/// <summary>
/// Actor が現在使う攻撃定義ID。
/// 将来は複数武器やスキルスロットへ拡張する入口になる。
/// </summary>
public struct AttackLoadout : IComponentData
{
    public AttackDefinitionId PrimaryAttack;
}
