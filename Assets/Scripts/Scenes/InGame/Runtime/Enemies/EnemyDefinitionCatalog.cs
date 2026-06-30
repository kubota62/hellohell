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
                return new EnemyDefinitionData
                {
                    TypeId = KiteEnemy,
                    Movement = EnemyMovementKind.Kite,
                    MaxHealth = 80,
                    MoveSpeed = 3.2f,
                    BodyScale = 0.9f,
                    HitRadius = 1.8f,
                    PrimaryAttack = AttackDefinitionId.BasicChainProjectile,
                    Color = new float4(0.1f, 0.35f, 1f, 1f),
                };

            case ChainEnemy:
                return new EnemyDefinitionData
                {
                    TypeId = ChainEnemy,
                    Movement = EnemyMovementKind.Forward,
                    MaxHealth = 120,
                    MoveSpeed = 2.1f,
                    BodyScale = 1.15f,
                    HitRadius = 2f,
                    PrimaryAttack = AttackDefinitionId.BasicChainProjectile,
                    Color = new float4(0.1f, 0.85f, 1f, 1f),
                };

            case RandomEnemy:
                return new EnemyDefinitionData
                {
                    TypeId = RandomEnemy,
                    Movement = EnemyMovementKind.Random,
                    MaxHealth = 100,
                    MoveSpeed = 2.4f,
                    BodyScale = 1f,
                    HitRadius = 2f,
                    PrimaryAttack = AttackDefinitionId.BasicProjectile,
                    Color = new float4(1f, 1f, 0f, 1f),
                };

            case ForwardEnemy:
            default:
                return new EnemyDefinitionData
                {
                    TypeId = ForwardEnemy,
                    Movement = EnemyMovementKind.Forward,
                    MaxHealth = 100,
                    MoveSpeed = 2.8f,
                    BodyScale = 1f,
                    HitRadius = 2f,
                    PrimaryAttack = AttackDefinitionId.BasicProjectile,
                    Color = new float4(1f, 0f, 1f, 1f),
                };
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
}
