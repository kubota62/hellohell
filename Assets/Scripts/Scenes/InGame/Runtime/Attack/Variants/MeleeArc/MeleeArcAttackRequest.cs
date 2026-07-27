using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Attack/Variants/MeleeArc 用の一回きりの扇状攻撃リクエスト。
/// 攻撃判定と見た目は別システムで処理し、攻撃種別の追加に備えて薄いデータだけを渡す。
/// </summary>
public struct MeleeArcAttackRequest : IComponentData
{
    public AttackMasterId AttackMasterId;
    public Entity Owner;
    public TeamId Team;
    public float3 Position;
    public float3 Direction;
    public float Radius;
    public float AngleDegrees;
    public int Damage;
    public byte IsCritical;
    public float VisualDuration;
}

/// <summary>
/// Attack/Variants/MeleeArc 用の表示リクエスト。
/// ロジック側を GameObject ベースの演出に依存させないため、攻撃判定後に別途発行する。
/// </summary>
public struct MeleeArcVfxRequest : IComponentData
{
    public float3 Position;
    public float3 Direction;
    public float Radius;
    public float AngleDegrees;
    public float Duration;
    public float4 Color;
}
