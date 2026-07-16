using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Enemyマスターの検索、スポーン抽選、アセット未登録時の標準値を提供する。
/// </summary>
public static class EnemyMasterCatalog
{
    public const int BasicSkeletonEnemy = 1;
    public const int MossSlimeEnemy = 2;
    public const int ArmoredSkeletonEnemy = 3;
    public const int GraveGuardEnemy = 4;
    public const int SkeletonDrifterEnemy = 5;
    public const int RatRunnerEnemy = 6;
    public const int GraveSlimeEnemy = 7;

    public static readonly int[] AllTypeIds =
    {
        BasicSkeletonEnemy,
        MossSlimeEnemy,
        ArmoredSkeletonEnemy,
        GraveGuardEnemy,
        SkeletonDrifterEnemy,
        RatRunnerEnemy,
        GraveSlimeEnemy,
    };

    public static int PickSpawnType(int spawnIndex)
    {
        var safeIndex = math.max(0, spawnIndex);
        return AllTypeIds[safeIndex % AllTypeIds.Length];
    }

    public static EnemyMasterData PickSpawnMaster(
        DynamicBuffer<EnemyMasterElement> definitions,
        int spawnIndex,
        int playerLevel)
    {
        if (definitions.Length == 0)
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

        var safeIndex = math.max(0, spawnIndex);
        if (totalWeight <= 0)
        {
            return definitions[safeIndex % definitions.Length].ToRuntimeMaster();
        }

        var targetWeight = safeIndex % totalWeight;
        var accumulatedWeight = 0;
        for (var i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i];
            if (!IsSpawnUnlocked(definition.Spawn, playerLevel))
            {
                continue;
            }

            accumulatedWeight += math.max(0, definition.Spawn.Weight);
            if (targetWeight < accumulatedWeight)
            {
                return definition.ToRuntimeMaster();
            }
        }

        return definitions[definitions.Length - 1].ToRuntimeMaster();
    }

    public static EnemyMasterData Get(int typeId)
    {
        switch (typeId)
        {
            case MossSlimeEnemy:
                return CreateContactEnemy(
                    typeId,
                    EnemyMovementKind.Forward,
                    maxHealth: 80,
                    hitRadius: 1.7f,
                    bodyScale: 0.9f,
                    moveSpeed: 0.85f,
                    attackRange: 2.6f,
                    color: new float4(0.4f, 0.48f, 0.36f, 1f),
                    experience: 1,
                    score: 12,
                    spawnWeight: 3,
                    minPlayerLevel: 4);

            case ArmoredSkeletonEnemy:
                return CreateContactEnemy(
                    typeId,
                    EnemyMovementKind.Forward,
                    maxHealth: 120,
                    hitRadius: 2f,
                    bodyScale: 1.15f,
                    moveSpeed: 0.95f,
                    attackRange: 2.8f,
                    color: new float4(0.48f, 0.5f, 0.52f, 1f),
                    experience: 2,
                    score: 20,
                    spawnWeight: 2,
                    minPlayerLevel: 5);

            case GraveGuardEnemy:
                return CreateContactEnemy(
                    typeId,
                    EnemyMovementKind.Forward,
                    maxHealth: 150,
                    hitRadius: 1.8f,
                    bodyScale: 1.15f,
                    moveSpeed: 0.8f,
                    attackRange: 2.9f,
                    color: new float4(0.3f, 0.32f, 0.38f, 1f),
                    experience: 3,
                    score: 26,
                    spawnWeight: 2,
                    minPlayerLevel: 7);

            case SkeletonDrifterEnemy:
                return CreateContactEnemy(
                    typeId,
                    EnemyMovementKind.Drift,
                    maxHealth: 60,
                    hitRadius: 1.25f,
                    bodyScale: 0.78f,
                    moveSpeed: 1.15f,
                    attackRange: 2.4f,
                    color: new float4(0.72f, 0.7f, 0.62f, 1f),
                    experience: 1,
                    score: 14,
                    spawnWeight: 8,
                    minPlayerLevel: 2);

            case RatRunnerEnemy:
                return CreateContactEnemy(
                    typeId,
                    EnemyMovementKind.Runner,
                    maxHealth: 28,
                    hitRadius: 0.9f,
                    bodyScale: 0.55f,
                    moveSpeed: 2.05f,
                    attackRange: 1.8f,
                    color: new float4(0.48f, 0.22f, 0.16f, 1f),
                    experience: 1,
                    score: 12,
                    spawnWeight: 5,
                    minPlayerLevel: 3);

            case GraveSlimeEnemy:
                return CreateContactEnemy(
                    typeId,
                    EnemyMovementKind.Heavy,
                    maxHealth: 160,
                    hitRadius: 2.1f,
                    bodyScale: 1.25f,
                    moveSpeed: 0.72f,
                    attackRange: 3.1f,
                    color: new float4(0.35f, 0.42f, 0.32f, 1f),
                    experience: 3,
                    score: 28,
                    spawnWeight: 3,
                    minPlayerLevel: 5);

            case BasicSkeletonEnemy:
            default:
                return CreateContactEnemy(
                    BasicSkeletonEnemy,
                    EnemyMovementKind.Forward,
                    maxHealth: 45,
                    hitRadius: 1.3f,
                    bodyScale: 0.75f,
                    moveSpeed: 1.35f,
                    attackRange: 2.4f,
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

    private static EnemyMasterData CreateContactEnemy(
        int typeId,
        EnemyMovementKind movement,
        int maxHealth,
        float hitRadius,
        float bodyScale,
        float moveSpeed,
        float attackRange,
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
                PrimaryAttack = AttackMasterId.BasicAura,
                MinAttackRange = 0f,
                MaxAttackRange = math.max(0f, attackRange),
            },
            Visual = new EnemyVisualMaster
            {
                Color = color,
            },
            Reward = new EnemyRewardMaster
            {
                Experience = math.max(0, experience),
                Score = math.max(0, score),
            },
            Spawn = new EnemySpawnMaster
            {
                Weight = math.max(0, spawnWeight),
                MinPlayerLevel = math.max(1, minPlayerLevel),
            },
        };
    }

    private static bool IsSpawnUnlocked(
        EnemySpawnMaster spawn,
        int playerLevel)
    {
        return playerLevel >= math.max(1, spawn.MinPlayerLevel);
    }
}
