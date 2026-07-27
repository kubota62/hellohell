using Unity.Entities;

/// <summary>
/// Player/Core/Skills の共通コンポーネント。
/// Playerが獲得したスキルの累積レベルと効果量を保持する。
/// 将来のスキル選択UIができるまでは、レベルアップ時に自動で各項目へ割り振る。
/// 効果量はマスタから加算しておき、攻撃や移動のSystemは計算済みの倍率だけを読む。
/// </summary>
public struct PlayerSkillStats : IComponentData
{
    public int DamageLevel;
    public int AttackSpeedLevel;
    public int MoveSpeedLevel;
    public int AreaLevel;
    public int RegenerationLevel;
    public int MaxHealthLevel;
    public int PickupRangeLevel;
    public int MeleeArcLevel;
    public int RapidBoltLevel;
    public int PiercingLanceLevel;
    public int ExplosiveOrbLevel;
    public int CriticalChanceLevel;
    public int CriticalDamageLevel;
    public int ArmorLevel;
    public float DamageMultiplierAdd;
    public float CooldownMultiplierReduction;
    public float MoveSpeedMultiplierAdd;
    public float AreaMultiplierAdd;
    public float HealthRegenerationPerSecond;
    public float MaxHealthAdd;
    public float PickupRadiusAdd;
    public float MeleeArcDamageMultiplierAdd;
    public float RapidBoltDamageMultiplierAdd;
    public float PiercingLanceDamageMultiplierAdd;
    public float ExplosiveOrbDamageMultiplierAdd;
    public float CriticalChance;
    public float CriticalDamageMultiplierAdd;
    public float DamageReduction;
}
