using Unity.Entities;

/// <summary>
/// Playerへ接近しながら横方向に揺れるEnemyを識別するタグコンポーネント。
/// </summary>
public struct EnemyMovementRandom : IComponentData
{
}

/// <summary>
/// Playerとの距離を保ちながら射撃位置を探すEnemyを識別するタグコンポーネント。
/// </summary>
public struct EnemyMovementKite : IComponentData
{
}
