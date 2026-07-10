using Unity.Entities;

/// <summary>
/// Attack/Core の共通コンポーネント。
/// Actor が現在使う攻撃マスタIDを持ち、将来的には複数武器やスキルスロットへ拡張する入口になる。
/// </summary>
public struct AttackLoadout : IComponentData
{
    public AttackMasterId PrimaryAttack;
}
