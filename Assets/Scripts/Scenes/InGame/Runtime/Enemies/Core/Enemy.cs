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
