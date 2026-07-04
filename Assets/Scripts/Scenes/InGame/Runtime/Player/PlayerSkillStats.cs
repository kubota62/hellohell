using Unity.Entities;

/// <summary>
/// Playerが獲得した仮スキルの累積レベル。
/// 将来のスキル選択UIができるまでは、レベルアップ時に自動で各項目へ割り振る。
/// </summary>
public struct PlayerSkillStats : IComponentData
{
    public int DamageLevel;
    public int AttackSpeedLevel;
    public int MoveSpeedLevel;
}
