using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 敵定義へアクセスするための仮カタログ。
/// Bakerが作った定義バッファを優先し、未配置でも既定値でゲームが動くようにする。
/// </summary>
public static class EnemyMasterCatalog
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

    public static EnemyMasterData PickSpawnMaster(
        DynamicBuffer<EnemyMasterElement> definitions,
        int spawnIndex,
        int playerLevel)
    {
        if (definitions.Length <= 0)
        {
            return Get(PickSpawnType(spawnIndex));
        }

        var totalWeight = 0;
        for (var i = 0; i < definitions.Length; i++)
        {
            if (IsSpawnUnlocked(definitions[i].Spawn, playerLevel))
            {
                totalWeight += math.max(0, definitions[i].Spawn.Weight);
            }
        }

        if (totalWeight <= 0)
        {
            var safeIndex = spawnIndex < 0 ? 0 : spawnIndex;
            return definitions[safeIndex % definitions.Length].ToRuntimeMaster();
        }

        var targetWeight = (spawnIndex < 0 ? 0 : spawnIndex) % totalWeight;
        var accumulatedWeight = 0;
        for (var i = 0; i < definitions.Length; i++)
        {
            if (!IsSpawnUnlocked(definitions[i].Spawn, playerLevel))
            {
                continue;
            }

            accumulatedWeight += math.max(0, definitions[i].Spawn.Weight);
            if (targetWeight < accumulatedWeight)
            {
                return definitions[i].ToRuntimeMaster();
            }
        }

        return definitions[definitions.Length - 1].ToRuntimeMaster();
    }

    public static EnemyMasterData Get(int typeId)
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
                    primaryAttack: AttackMasterId.BasicChainProjectile,
                    minAttackRange: 8f,
                    maxAttackRange: 24f,
                    color: new float4(0.1f, 0.35f, 1f, 1f),
                    experience: 2,
                    score: 18,
                    spawnWeight: 2,
                    minPlayerLevel: 7);

            case ChainEnemy:
                return Create(
                    ChainEnemy,
                    EnemyMovementKind.Forward,
                    maxHealth: 120,
                    hitRadius: 2f,
                    bodyScale: 1.15f,
                    moveSpeed: 2.1f,
                    primaryAttack: AttackMasterId.BasicChainProjectile,
                    minAttackRange: 5f,
                    maxAttackRange: 22f,
                    color: new float4(0.1f, 0.85f, 1f, 1f),
                    experience: 2,
                    score: 20,
                    spawnWeight: 2,
                    minPlayerLevel: 5);

            case RandomEnemy:
                return Create(
                    RandomEnemy,
                    EnemyMovementKind.Random,
                    maxHealth: 100,
                    hitRadius: 2f,
                    bodyScale: 1f,
                    moveSpeed: 2.4f,
                    primaryAttack: AttackMasterId.BasicProjectile,
                    minAttackRange: 5f,
                    maxAttackRange: 24f,
                    color: new float4(1f, 1f, 0f, 1f),
                    experience: 1,
                    score: 12,
                    spawnWeight: 3,
                    minPlayerLevel: 4);

            case ForwardEnemy:
            default:
                return Create(
                    ForwardEnemy,
                    EnemyMovementKind.Forward,
                    maxHealth: 45,
                    hitRadius: 1.3f,
                    bodyScale: 0.75f,
                    moveSpeed: 1.35f,
                    primaryAttack: AttackMasterId.BasicAura,
                    minAttackRange: 0f,
                    maxAttackRange: 2.4f,
                    color: new float4(0.42f, 0.5f, 0.32f, 1f),
                    experience: 1,
                    score: 10,
                    spawnWeight: 10,
                    minPlayerLevel: 1);
        }
    }

    public static EnemyMasterData Get(
        DynamicBuffer<EnemyMasterElement> definitions,
        int typeId)
    {
        for (var i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].TypeId == typeId)
            {
                return definitions[i].ToRuntimeMaster();
            }
        }

        return Get(typeId);
    }

    private static EnemyMasterData Create(
        int typeId,
        EnemyMovementKind movement,
        int maxHealth,
        float hitRadius,
        float bodyScale,
        float moveSpeed,
        AttackMasterId primaryAttack,
        float minAttackRange,
        float maxAttackRange,
        float4 color,
        int experience,
        int score,
        int spawnWeight,
        int minPlayerLevel)
    {
        return new EnemyMasterData
        {
            TypeId = typeId,
            Stats = new EnemyStatMaster
            {
                MaxHealth = maxHealth,
                HitRadius = hitRadius,
                BodyScale = bodyScale,
            },
            Movement = new EnemyMovementMaster
            {
                Kind = movement,
                MoveSpeed = moveSpeed,
            },
            Combat = new EnemyCombatMaster
            {
                PrimaryAttack = primaryAttack,
                MinAttackRange = minAttackRange,
                MaxAttackRange = math.max(minAttackRange, maxAttackRange),
            },
            Visual = new EnemyVisualMaster
            {
                Color = color,
            },
            Reward = new EnemyRewardMaster
            {
                Experience = experience,
                Score = score,
            },
            Spawn = new EnemySpawnMaster
            {
                Weight = math.max(0, spawnWeight),
                MinPlayerLevel = math.max(1, minPlayerLevel),
            },
        };
    }

    private static bool IsSpawnUnlocked(EnemySpawnMaster spawn, int playerLevel)
    {
        var requiredLevel = spawn.MinPlayerLevel <= 0 ? 1 : spawn.MinPlayerLevel;
        return playerLevel >= requiredLevel;
    }
}
