using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 敵の種類、移動、初期ステータス、攻撃をまとめて管理するScriptableObject。
/// Inspector上では調整しやすい平らな項目にし、ECSへ渡す時にカテゴリ構造へ詰め替える。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Masters/Enemy Master")]
public class EnemyMasterAsset : ScriptableObject
{
    [Header("Identity")]
    public int TypeId = 1;

    [Header("Stats")]
    public int MaxHealth = 100;
    public float HitRadius = 2f;
    public float BodyScale = 1f;

    [Header("Movement")]
    public EnemyMovementKind Movement = EnemyMovementKind.Forward;
    public float MoveSpeed = 2.8f;

    [Header("Combat")]
    public AttackMasterId PrimaryAttack = AttackMasterId.BasicProjectile;
    public float MinAttackRange = 5f;
    public float MaxAttackRange = 24f;

    [Header("Visual")]
    public Color Color = UnityEngine.Color.magenta;

    [Header("Reward")]
    public int Experience = 1;
    public int Score = 10;

    public EnemyMasterData ToRuntimeMaster()
    {
        var fallback = EnemyMasterCatalog.Get(TypeId);
        var moveSpeed = MoveSpeed > 0f ? MoveSpeed : fallback.Movement.MoveSpeed;
        var bodyScale = BodyScale > 0f ? BodyScale : fallback.Stats.BodyScale;
        var minAttackRange = MinAttackRange > 0f ? MinAttackRange : fallback.Combat.MinAttackRange;
        var maxAttackRange = MaxAttackRange > 0f ? MaxAttackRange : fallback.Combat.MaxAttackRange;

        return new EnemyMasterData
        {
            TypeId = TypeId,
            Stats = new EnemyStatMaster
            {
                MaxHealth = MaxHealth,
                HitRadius = HitRadius,
                BodyScale = bodyScale,
            },
            Movement = new EnemyMovementMaster
            {
                Kind = Movement,
                MoveSpeed = moveSpeed,
            },
            Combat = new EnemyCombatMaster
            {
                PrimaryAttack = PrimaryAttack,
                MinAttackRange = minAttackRange,
                MaxAttackRange = math.max(minAttackRange, maxAttackRange),
            },
            Visual = new EnemyVisualMaster
            {
                Color = new float4(Color.r, Color.g, Color.b, Color.a),
            },
            Reward = new EnemyRewardMaster
            {
                Experience = Experience > 0 ? Experience : fallback.Reward.Experience,
                Score = Score > 0 ? Score : fallback.Reward.Score,
            },
        };
    }
}
