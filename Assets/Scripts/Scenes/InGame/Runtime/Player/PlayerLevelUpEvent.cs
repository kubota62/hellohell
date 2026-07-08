using Unity.Entities;

/// <summary>
/// Playerがレベルアップした時に一度だけ発行されるイベント。
/// スキル選択、演出、SEなどはこのイベントを起点に後から接続する。
/// </summary>
public struct PlayerLevelUpEvent : IComponentData
{
    public Entity Player;
    public int NewLevel;
    public int LevelsGained;
}

/// <summary>
/// Playerへスキル強化が適用された時に一度だけ発行されるイベント。
/// 将来の取得演出、SE、HUDログはこのイベントから接続する。
/// </summary>
public struct PlayerSkillAppliedEvent : IComponentData
{
    public Entity Player;
    public PlayerSkillMasterId SkillId;
    public PlayerSkillKind Kind;
    public int AddedLevel;
    public int NewSkillLevel;
}
