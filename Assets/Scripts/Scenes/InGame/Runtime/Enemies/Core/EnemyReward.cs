using Unity.Entities;

/// <summary>
/// Enemies/Core の共通コンポーネント。
/// EnemyMasterから注入される敵ごとの報酬値。
/// 死亡時にEnemyRewardEventへ変換され、経験値やスコアなどの消費側へ渡される。
/// </summary>
public struct EnemyReward : IComponentData
{
    public int Experience;
    public int Score;
}

/// <summary>
/// Enemies/Core の共通イベント。
/// 敵が倒された時に一度だけ発行される報酬イベント。
/// 現時点では消費先を固定せず、プレイヤー成長やスコア実装から購読できる形にしておく。
/// </summary>
public struct EnemyRewardEvent : IComponentData
{
    public int EnemyTypeId;
    public int Experience;
    public int Score;
}

/// <summary>
/// 敵の死亡地点に残り、プレイヤーが近づくと吸い寄せられる経験値ドロップ。
/// </summary>
public struct ExperiencePickup : IComponentData
{
    public int Experience;
    public int Score;
}
