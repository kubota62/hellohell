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
}

/// <summary>
/// レベルアップ時に適用するスキル強化のランタイム用マスタ値。
/// 選択UIが入るまではWeightを使って自動選択の出現比率として扱う。
/// MaxLevelは同じスキルを積める上限で、0以下なら上限なしとして扱う。
/// </summary>
public struct PlayerSkillMasterData
{
    public PlayerSkillMasterId Id;
    public PlayerSkillKind Kind;
    public int AddLevel;
    public int MaxLevel;
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
    public int Weight;

    public static PlayerSkillMasterElement FromMaster(PlayerSkillMasterData master)
    {
        return new PlayerSkillMasterElement
        {
            Id = master.Id,
            Kind = master.Kind,
            AddLevel = master.AddLevel,
            MaxLevel = master.MaxLevel,
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
            Weight = Weight,
        };
    }
}
