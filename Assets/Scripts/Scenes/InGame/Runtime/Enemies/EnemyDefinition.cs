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
/// HP、当たり判定、見た目サイズなど、敵の身体的な基本値。
/// </summary>
public struct EnemyStatDefinition
{
    public int MaxHealth;
    public float HitRadius;
    public float BodyScale;
}

/// <summary>
/// どのAIで動くかと、そのAIが使う移動速度。
/// </summary>
public struct EnemyMovementDefinition
{
    public EnemyMovementKind Kind;
    public float MoveSpeed;
}

/// <summary>
/// 敵が使う攻撃と、攻撃可能な距離帯。
/// </summary>
public struct EnemyCombatDefinition
{
    public AttackDefinitionId PrimaryAttack;
    public float MinAttackRange;
    public float MaxAttackRange;
}

/// <summary>
/// 色など、敵の見た目に関わる軽量な値。
/// </summary>
public struct EnemyVisualDefinition
{
    public float4 Color;
}

/// <summary>
/// ランタイムで参照する敵定義の軽量データ。
/// ScriptableObjectから焼き込まれた値を、Actor生成時にカテゴリごとへ適用する。
/// </summary>
public struct EnemyDefinitionData
{
    public int TypeId;
    public EnemyStatDefinition Stats;
    public EnemyMovementDefinition Movement;
    public EnemyCombatDefinition Combat;
    public EnemyVisualDefinition Visual;
}

/// <summary>
/// EnemyDefinitionから注入される敵ごとの攻撃可能距離。
/// AIの移動距離とは別に持たせ、同じ移動パターンでも近接型や射撃型へ調整できるようにする。
/// </summary>
public struct EnemyAttackRange : IComponentData
{
    public float Min;
    public float Max;
}
