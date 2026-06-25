using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Aura 攻撃を解決するための一回限りのリクエスト。
/// 現時点では範囲内の敵へ即時ダメージを与え、将来的な演出は VfxRequest 側へ分離する。
/// </summary>
public struct AuraAttackRequest : IComponentData
{
    public AttackDefinitionId AttackDefinitionId;
    public Entity Owner;
    public TeamId Team;
    public float3 Position;
    public float Radius;
    public int Damage;
}
