using Unity.Entities;

/// <summary>
/// レベルアップ中にプレイヤーへ提示する3つの強化候補。
/// PendingLevels が残っている場合は、選択後に次の候補を続けて提示する。
/// </summary>
public struct PlayerUpgradeChoice : IComponentData
{
    public Entity Player;
    public PlayerSkillMasterData First;
    public PlayerSkillMasterData Second;
    public PlayerSkillMasterData Third;
    public int NewLevel;
    public int PendingLevels;
    public int RerollsRemaining;
    public int RerollGeneration;
}

/// <summary>
/// Managed入力からECSへ渡すレベルアップ選択結果。ChoiceIndex は0～2。
/// </summary>
public struct PlayerUpgradeSelection : IComponentData
{
    public int ChoiceIndex;
}

public struct PlayerUpgradeReroll : IComponentData
{
}
