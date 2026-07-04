using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 敵定義へアクセスするための仮カタログ。
/// Bakerが作った定義バッファを優先し、未配置でも既定値でゲームが動くようにする。
/// </summary>
public static class EnemyDefinitionCatalog
{
    public const int ForwardEnemy = 1;
    public const int RandomEnemy = 2;
    public const int ChainEnemy = 3;
    public const int KiteEnemy = 4;

    public static int PickSpawnType(int spawnIndex)
    {
        switch (spawnIndex % 4)
        {
            case 1:
                return RandomEnemy;

            case 2:
                return ChainEnemy;

            case 3:
                return KiteEnemy;

            case 0:
            default:
                return ForwardEnemy;
        }
    }

    public static EnemyDefinitionData PickSpawnDefinition(
        DynamicBuffer<EnemyDefinitionElement> definitions,
        int spawnIndex)
    {
        if (definitions.Length <= 0)
        {
            return Get(PickSpawnType(spawnIndex));
        }

        var safeIndex = spawnIndex < 0 ? 0 : spawnIndex;
        return definitions[safeIndex % definitions.Length].ToRuntimeDefinition();
    }

    public static EnemyDefinitionData Get(int typeId)
    {
        switch (typeId)
        {
            case KiteEnemy:
                return Create(
                    KiteEnemy,
                    EnemyMovementKind.Kite,
                    maxHealth: 80,
                    hitRadius: 1.8f,
                    bodyScale: 0.9f,
                    moveSpeed: 3.2f,
                    primaryAttack: AttackDefinitionId.BasicChainProjectile,
                    minAttackRange: 8f,
                    maxAttackRange: 24f,
                    color: new float4(0.1f, 0.35f, 1f, 1f));

            case ChainEnemy:
                return Create(
                    ChainEnemy,
                    EnemyMovementKind.Forward,
                    maxHealth: 120,
                    hitRadius: 2f,
                    bodyScale: 1.15f,
                    moveSpeed: 2.1f,
                    primaryAttack: AttackDefinitionId.BasicChainProjectile,
                    minAttackRange: 5f,
                    maxAttackRange: 22f,
                    color: new float4(0.1f, 0.85f, 1f, 1f));

            case RandomEnemy:
                return Create(
                    RandomEnemy,
                    EnemyMovementKind.Random,
                    maxHealth: 100,
                    hitRadius: 2f,
                    bodyScale: 1f,
                    moveSpeed: 2.4f,
                    primaryAttack: AttackDefinitionId.BasicProjectile,
                    minAttackRange: 5f,
                    maxAttackRange: 24f,
                    color: new float4(1f, 1f, 0f, 1f));

            case ForwardEnemy:
            default:
                return Create(
                    ForwardEnemy,
                    EnemyMovementKind.Forward,
                    maxHealth: 100,
                    hitRadius: 2f,
                    bodyScale: 1f,
                    moveSpeed: 2.8f,
                    primaryAttack: AttackDefinitionId.BasicProjectile,
                    minAttackRange: 5f,
                    maxAttackRange: 24f,
                    color: new float4(1f, 0f, 1f, 1f));
        }
    }

    public static EnemyDefinitionData Get(
        DynamicBuffer<EnemyDefinitionElement> definitions,
        int typeId)
    {
        for (var i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].TypeId == typeId)
            {
                return definitions[i].ToRuntimeDefinition();
            }
        }

        return Get(typeId);
    }

    private static EnemyDefinitionData Create(
        int typeId,
        EnemyMovementKind movement,
        int maxHealth,
        float hitRadius,
        float bodyScale,
        float moveSpeed,
        AttackDefinitionId primaryAttack,
        float minAttackRange,
        float maxAttackRange,
        float4 color)
    {
        return new EnemyDefinitionData
        {
            TypeId = typeId,
            Stats = new EnemyStatDefinition
            {
                MaxHealth = maxHealth,
                HitRadius = hitRadius,
                BodyScale = bodyScale,
            },
            Movement = new EnemyMovementDefinition
            {
                Kind = movement,
                MoveSpeed = moveSpeed,
            },
            Combat = new EnemyCombatDefinition
            {
                PrimaryAttack = primaryAttack,
                MinAttackRange = minAttackRange,
                MaxAttackRange = math.max(minAttackRange, maxAttackRange),
            },
            Visual = new EnemyVisualDefinition
            {
                Color = color,
            },
        };
    }
}
