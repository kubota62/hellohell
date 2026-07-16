using Unity.Entities;

/// <summary>Enemyが使用する接近パターン。</summary>
public struct EnemyMovementPattern : IComponentData
{
    public EnemyMovementKind Kind;
}
