using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Baker が ScriptableObject の敵定義を ECS 側へ渡すためのバッファ要素。
/// スポーンシステムは managed な ScriptableObject ではなく、この値だけを読む。
/// </summary>
public struct EnemyDefinitionElement : IBufferElementData
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

    public static EnemyDefinitionElement FromDefinition(EnemyDefinitionData definition)
    {
        return new EnemyDefinitionElement
        {
            TypeId = definition.TypeId,
            Movement = definition.Movement,
            MaxHealth = definition.MaxHealth,
            MoveSpeed = definition.MoveSpeed,
            BodyScale = definition.BodyScale,
            HitRadius = definition.HitRadius,
            MinAttackRange = definition.MinAttackRange,
            MaxAttackRange = definition.MaxAttackRange,
            PrimaryAttack = definition.PrimaryAttack,
            Color = definition.Color,
        };
    }

    public EnemyDefinitionData ToRuntimeDefinition()
    {
        return new EnemyDefinitionData
        {
            TypeId = TypeId,
            Movement = Movement,
            MaxHealth = MaxHealth,
            MoveSpeed = MoveSpeed,
            BodyScale = BodyScale,
            HitRadius = HitRadius,
            MinAttackRange = MinAttackRange,
            MaxAttackRange = MaxAttackRange,
            PrimaryAttack = PrimaryAttack,
            Color = Color,
        };
    }
}
