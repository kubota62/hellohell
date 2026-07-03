using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 敵定義が選ぶ移動パターン。
/// 敵の種類が増えても、移動ロジックと数値定義を分けて扱えるようにする。
/// </summary>
public enum EnemyMovementKind : byte
{
    Forward = 1,
    Random = 2,
    Kite = 3,
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
    public float MoveSpeed;
    public float BodyScale;
    public float HitRadius;
    public float MinAttackRange;
    public float MaxAttackRange;
    public AttackDefinitionId PrimaryAttack;
    public float4 Color;
}

/// <summary>
/// EnemyDefinition から注入される敵ごとの攻撃可能距離。
/// AIの移動距離とは別に持たせ、同じ移動パターンでも近接型や射撃型へ調整できるようにする。
/// </summary>
public struct EnemyAttackRange : IComponentData
{
    public float Min;
    public float Max;
}
