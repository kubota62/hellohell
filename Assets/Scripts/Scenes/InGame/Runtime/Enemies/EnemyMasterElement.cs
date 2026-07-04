using Unity.Entities;

/// <summary>
/// BakerがScriptableObjectの敵定義をECS側へ渡すためのバッファ要素。
/// スポーンシステムはmanagedなScriptableObjectではなく、この値だけを読む。
/// </summary>
public struct EnemyMasterElement : IBufferElementData
{
    public int TypeId;
    public EnemyStatMaster Stats;
    public EnemyMovementMaster Movement;
    public EnemyCombatMaster Combat;
    public EnemyVisualMaster Visual;

    public static EnemyMasterElement FromMaster(EnemyMasterData definition)
    {
        return new EnemyMasterElement
        {
            TypeId = definition.TypeId,
            Stats = definition.Stats,
            Movement = definition.Movement,
            Combat = definition.Combat,
            Visual = definition.Visual,
        };
    }

    public EnemyMasterData ToRuntimeMaster()
    {
        return new EnemyMasterData
        {
            TypeId = TypeId,
            Stats = Stats,
            Movement = Movement,
            Combat = Combat,
            Visual = Visual,
        };
    }
}
