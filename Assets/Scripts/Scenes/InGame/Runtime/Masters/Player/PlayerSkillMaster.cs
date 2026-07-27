using Unity.Entities;

/// <summary>
/// プレイヤースキルマスタを参照するための軽量ID。
/// 将来のスキル選択UIや進化スキルは、このIDを選択結果として扱う。
/// </summary>
public enum PlayerSkillMasterId
{
    DamageBoost = 1,
    AttackSpeedBoost = 2,
    MoveSpeedBoost = 3,
    AreaBoost = 4,
    RegenerationBoost = 5,
    MaxHealthBoost = 6,
    PickupRangeBoost = 7,
    MeleeArcMastery = 8,
    RapidBoltMastery = 9,
    PiercingLanceMastery = 10,
    ExplosiveOrbMastery = 11,
    CriticalChanceBoost = 12,
    CriticalDamageBoost = 13,
    ArmorBoost = 14,
    MultistrikeBoost = 15,
    ExecutionerBoost = 16,
}

/// <summary>
/// スキルが強化する対象ステータス。
/// 効果量の計算はPlayerSkillStatsへ集約し、攻撃Systemや移動Systemは倍率だけを読む。
/// </summary>
public enum PlayerSkillKind : byte
{
    Damage = 1,
    AttackSpeed = 2,
    MoveSpeed = 3,
    Area = 4,
    Regeneration = 5,
    MaxHealth = 6,
    PickupRange = 7,
    MeleeArc = 8,
    RapidBolt = 9,
    PiercingLance = 10,
    ExplosiveOrb = 11,
    CriticalChance = 12,
    CriticalDamage = 13,
    Armor = 14,
    Multistrike = 15,
    Executioner = 16,
}

/// <summary>
/// レベルアップ時に適用するスキル強化のランタイム用マスタ値。
/// 選択UIが入るまではWeightを使って自動選択の出現比率として扱う。
/// MaxLevelは同じスキルを積める上限で、0以下なら上限なしとして扱う。
/// EffectPerLevelはKindごとの倍率増加量として扱う。
/// </summary>
public struct PlayerSkillMasterData
{
    public PlayerSkillMasterId Id;
    public PlayerSkillKind Kind;
    public int AddLevel;
    public int MaxLevel;
    public float EffectPerLevel;
    public int Weight;
}

/// <summary>
/// BakerがScriptableObjectのスキルマスタをECS側へ渡すためのバッファ要素。
/// </summary>
public struct PlayerSkillMasterElement : IBufferElementData
{
    public PlayerSkillMasterId Id;
    public PlayerSkillKind Kind;
    public int AddLevel;
    public int MaxLevel;
    public float EffectPerLevel;
    public int Weight;

    public static PlayerSkillMasterElement FromMaster(PlayerSkillMasterData master)
    {
        return new PlayerSkillMasterElement
        {
            Id = master.Id,
            Kind = master.Kind,
            AddLevel = master.AddLevel,
            MaxLevel = master.MaxLevel,
            EffectPerLevel = master.EffectPerLevel,
            Weight = master.Weight,
        };
    }

    public PlayerSkillMasterData ToRuntimeMaster()
    {
        return new PlayerSkillMasterData
        {
            Id = Id,
            Kind = Kind,
            AddLevel = AddLevel,
            MaxLevel = MaxLevel,
            EffectPerLevel = EffectPerLevel,
            Weight = Weight,
        };
    }
}
