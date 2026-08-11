using Unity.Entities;

public enum PlayerUpgradeSource : byte
{
    LevelUp = 0,
    ChampionTreasure = 1,
}

/// <summary>
/// Player/Core/Progress の共通イベント。
/// Playerがレベルアップした時に一度だけ発行される。
/// スキル選択、演出、SEなどはこのイベントを起点に後から接続する。
/// </summary>
public struct PlayerLevelUpEvent : IComponentData
{
    public Entity Player;
    public int NewLevel;
    public int LevelsGained;
    public PlayerUpgradeSource Source;
}
