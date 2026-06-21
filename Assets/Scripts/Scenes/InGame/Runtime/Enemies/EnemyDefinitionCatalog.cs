using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 敵定義へアクセスする仮の窓口。
/// Baker が作った定義バッファがある場合はそちらを優先し、未配置でも静的な既定値で動く。
/// </summary>
public static class EnemyDefinitionCatalog
{
    public const int ForwardEnemy = 1;
    public const int RandomEnemy = 2;

    public static int PickSpawnType(int spawnIndex)
    {
        return spawnIndex % 2 == 0 ? RandomEnemy : ForwardEnemy;
    }

    public static EnemyDefinitionData Get(int typeId)
    {
        switch (typeId)
        {
            case RandomEnemy:
                return new EnemyDefinitionData
                {
                    TypeId = RandomEnemy,
                    Movement = EnemyMovementKind.Random,
                    MaxHealth = 100,
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
