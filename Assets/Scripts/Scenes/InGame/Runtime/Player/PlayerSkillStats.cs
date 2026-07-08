using Unity.Entities;

/// <summary>
/// Playerが獲得した仮スキルの累積レベル。
/// 将来のスキル選択UIができるまでは、レベルアップ時に自動で各項目へ割り振る。
/// 効果量はマスタから加算しておき、攻撃や移動のSystemは計算済みの倍率だけを読む。
/// </summary>
public struct PlayerSkillStats : IComponentData
{
    public int DamageLevel;
    public int AttackSpeedLevel;
    public int MoveSpeedLevel;
    public float DamageMultiplierAdd;
    public float CooldownMultiplierReduction;
    public float MoveSpeedMultiplierAdd;
}
