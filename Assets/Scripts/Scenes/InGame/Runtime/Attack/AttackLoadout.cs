using Unity.Entities;

/// <summary>
/// Actor が現在使う攻撃定義ID。
/// 将来的には複数武器やスキルスロットへ拡張する入口になる。
/// </summary>
public struct AttackLoadout : IComponentData
{
    public AttackMasterId PrimaryAttack;
}
