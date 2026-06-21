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
    public float HitRadius;
    public AttackDefinitionId PrimaryAttack;
    public float4 Color;

    public static EnemyDefinitionElement FromDefinition(EnemyDefinitionData definition)
    {
        return new EnemyDefinitionElement
        {
            TypeId = definition.TypeId,
            Movement = definition.Movement,
            MaxHealth = definition.MaxHealth,
            HitRadius = definition.HitRadius,
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
            HitRadius = HitRadius,
            PrimaryAttack = PrimaryAttack,
            Color = Color,
        };
    }
}
