using Unity.Entities;

/// <summary>
/// Enemies/Core の共通タグ。
/// エンティティを Enemy として扱うために付与する。
/// Team は敵対関係、Enemy は敵用システムで動く Actor かどうかを表す。
/// </summary>
public struct Enemy : IComponentData
{
}

/// <summary>
/// 通常敵より高い能力と報酬を持つ、ラン中の節目となる強敵。
/// </summary>
public struct EliteEnemy : IComponentData
{
}

/// <summary>
/// 一定時間ごとに出現する中ボス級の強敵。
/// EliteEnemyも併せて持ち、既存の強敵向け処理へ参加する。
/// </summary>
public struct ChampionEnemy : IComponentData
{
}
