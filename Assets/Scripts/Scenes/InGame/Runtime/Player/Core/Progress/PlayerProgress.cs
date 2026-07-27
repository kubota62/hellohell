using Unity.Entities;

/// <summary>
/// Player/Core/Progress の共通コンポーネント。
/// Playerの成長状態とスコアを保持する。
/// 敵死亡時のEnemyRewardEventを集計し、将来のスキル選択やレベルアップ演出の入口にする。
/// </summary>
public struct PlayerProgress : IComponentData
{
    public int Level;
    public int Experience;
    public int ExperienceToNextLevel;
    public int Score;
}

/// <summary>
/// Enemies can leave a restorative shard behind. It is attracted by the
/// same pickup-range build stat as experience gems.
/// </summary>
public struct HealthPickup : IComponentData
{
    public int Healing;
}

/// <summary>One-frame feedback emitted after restorative shards heal the player.</summary>
public struct PlayerHealedEvent : IComponentData
{
    public int Amount;
}
