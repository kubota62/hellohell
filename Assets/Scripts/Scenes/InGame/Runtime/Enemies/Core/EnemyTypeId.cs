using Unity.Entities;

/// <summary>
/// Enemies/Core の共通コンポーネント。
/// EnemyMaster を参照するための軽量ID。
/// 将来の EnemyMaster Baker が、このIDを移動、見た目、武器、報酬へ対応付ける。
/// </summary>
public struct EnemyTypeId : IComponentData
{
    public int Value;
}
