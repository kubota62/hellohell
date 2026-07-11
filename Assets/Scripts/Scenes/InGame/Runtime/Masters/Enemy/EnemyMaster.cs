using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Masters/Enemy の移動方式定義。
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
public struct EnemyStatMaster
{
    public int MaxHealth;
    public float HitRadius;
    public float BodyScale;
}

/// <summary>
/// どのAIで動くかと、そのAIが使う移動速度。
/// </summary>
public struct EnemyMovementMaster
{
    public EnemyMovementKind Kind;
    public float MoveSpeed;
}

/// <summary>
/// 敵が使う攻撃と、攻撃可能な距離帯。
/// </summary>
public struct EnemyCombatMaster
{
    public AttackMasterId PrimaryAttack;
    public float MinAttackRange;
    public float MaxAttackRange;
}

/// <summary>
/// 色など、敵の見た目に関わる軽量な値。
/// </summary>
public struct EnemyVisualMaster
{
    public float4 Color;
}

/// <summary>
/// 敵を倒した時に発生する経験値やスコアなどの報酬値。
/// </summary>
public struct EnemyRewardMaster
{
    public int Experience;
    public int Score;
}

/// <summary>
/// 敵の出現制御に関わる値。
/// 種類ごとの出やすさと解放レベルをマスタに寄せ、スポーンシステム側の分岐を増やさず調整できるようにする。
/// </summary>
public struct EnemySpawnMaster
{
    public int Weight;
    public int MinPlayerLevel;
}

/// <summary>
/// ランタイムで参照する敵定義の軽量データ。
/// ScriptableObjectから焼き込まれた値を、Actor生成時にカテゴリごとへ適用する。
/// </summary>
public struct EnemyMasterData
{
    public int TypeId;
    public EnemyStatMaster Stats;
    public EnemyMovementMaster Movement;
    public EnemyCombatMaster Combat;
    public EnemyVisualMaster Visual;
    public EnemyRewardMaster Reward;
    public EnemySpawnMaster Spawn;
}

/// <summary>
/// EnemyMasterから注入される敵ごとの攻撃可能距離。
/// AIの移動距離とは別に持たせ、同じ移動パターンでも近接型や射撃型へ調整できるようにする。
/// </summary>
public struct EnemyAttackRange : IComponentData
{
    public float Min;
    public float Max;
}
