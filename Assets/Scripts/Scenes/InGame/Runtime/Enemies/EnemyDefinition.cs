using Unity.Mathematics;

/// <summary>
/// 敵定義が選ぶ移動パターン。
/// まずは既存の Forward / Random を定義データから切り替える。
/// </summary>
public enum EnemyMovementKind : byte
{
    Forward = 1,
    Random = 2,
}

/// <summary>
/// ランタイムで使う敵定義の軽量データ。
/// ScriptableObject や BlobAsset から作る最終形を想定して、Actor 生成時の値をまとめる。
/// </summary>
public struct EnemyDefinitionData
{
    public int TypeId;
    public EnemyMovementKind Movement;
    public int MaxHealth;
    public float HitRadius;
    public AttackDefinitionId PrimaryAttack;
    public float4 Color;
}
