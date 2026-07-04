using Unity.Entities;

/// <summary>
/// BakerがScriptableObjectの敵定義をECS側へ渡すためのバッファ要素。
/// スポーンシステムはmanagedなScriptableObjectではなく、この値だけを読む。
/// </summary>
public struct EnemyDefinitionElement : IBufferElementData
{
    public int TypeId;
    public EnemyStatDefinition Stats;
    public EnemyMovementDefinition Movement;
    public EnemyCombatDefinition Combat;
    public EnemyVisualDefinition Visual;

    public static EnemyDefinitionElement FromDefinition(EnemyDefinitionData definition)
    {
        return new EnemyDefinitionElement
        {
            TypeId = definition.TypeId,
            Stats = definition.Stats,
            Movement = definition.Movement,
            Combat = definition.Combat,
            Visual = definition.Visual,
        };
    }

    public EnemyDefinitionData ToRuntimeDefinition()
    {
        return new EnemyDefinitionData
        {
            TypeId = TypeId,
            Stats = Stats,
            Movement = Movement,
            Combat = Combat,
            Visual = Visual,
        };
    }
}
