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
