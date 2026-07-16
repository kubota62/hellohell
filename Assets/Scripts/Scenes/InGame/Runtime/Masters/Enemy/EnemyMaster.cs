using Unity.Entities;
using Unity.Mathematics;

/// <summary>Enemyの接近方法。</summary>
public enum EnemyMovementKind : byte
{
    Forward = 1,
    Random = 2,
    Kite = 3,
    Drift = 4,
    Runner = 5,
    Heavy = 6,
}

public struct EnemyStatMaster
{
    public int MaxHealth;
    public float HitRadius;
    public float BodyScale;
}

public struct EnemyMovementMaster
{
    public EnemyMovementKind Kind;
    public float MoveSpeed;
}

public struct EnemyCombatMaster
{
    public AttackMasterId PrimaryAttack;
    public float MinAttackRange;
    public float MaxAttackRange;
}

public struct EnemyVisualMaster
{
    public float4 Color;
}

public struct EnemyRewardMaster
{
    public int Experience;
    public int Score;
}

public struct EnemySpawnMaster
{
    public int Weight;
    public int MinPlayerLevel;
}

/// <summary>Enemy一種類分の実行時設定。</summary>
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

/// <summary>Enemyが攻撃可能なPlayerとの距離範囲。</summary>
public struct EnemyAttackRange : IComponentData
{
    public float Min;
    public float Max;
}
