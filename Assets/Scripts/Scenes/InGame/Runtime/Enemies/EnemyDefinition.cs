using Unity.Mathematics;

/// <summary>
/// 敵定義が選ぶ移動パターン。
/// 敵の種類が増えても、移動ロジックと数値定義を分けて扱えるようにする。
/// </summary>
public enum EnemyMovementKind : byte
{
    Forward = 1,
    Random = 2,
}

/// <summary>
/// ランタイムで参照する敵定義の軽量データ。
/// ScriptableObjectから焼き込まれた値を、Actor生成時にまとめて適用する。
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
