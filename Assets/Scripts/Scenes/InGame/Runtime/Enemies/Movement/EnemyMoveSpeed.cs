using Unity.Entities;

/// <summary>
/// EnemyDefinition から注入される敵ごとの移動速度。
/// 移動パターンとは独立させ、同じAIでも速い敵や遅い敵を作れるようにする。
/// </summary>
public struct EnemyMoveSpeed : IComponentData
{
    public float Value;
}
