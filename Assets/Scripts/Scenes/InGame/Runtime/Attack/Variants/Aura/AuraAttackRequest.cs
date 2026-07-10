using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Attack/Variants/Aura 用の一回限りの範囲攻撃リクエスト。
/// 現時点では範囲内の敵へ即時ダメージを与え、将来的な演出は VfxRequest 側へ分離する。
/// </summary>
public struct AuraAttackRequest : IComponentData
{
    public AttackMasterId AttackMasterId;
    public Entity Owner;
    public TeamId Team;
    public float3 Position;
    public float Radius;
    public int Damage;
}
