using Unity.Entities;

/// <summary>
/// Playerの成長状態とスコアを保持するコンポーネント。
/// 敵死亡時のEnemyRewardEventを集計し、将来のスキル選択やレベルアップ演出の入口にする。
/// </summary>
public struct PlayerProgress : IComponentData
{
    public int Level;
    public int Experience;
    public int ExperienceToNextLevel;
    public int Score;
}
