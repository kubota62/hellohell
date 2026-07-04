using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 敵の種類、移動、初期ステータス、攻撃をまとめて管理するScriptableObject。
/// Inspector上では調整しやすいように平らな項目にし、ECSへ渡す時にカテゴリ構造へ詰め替える。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Definitions/Enemy Definition")]
public class EnemyDefinitionAsset : ScriptableObject
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
    public AttackDefinitionId PrimaryAttack = AttackDefinitionId.BasicProjectile;
    public float MinAttackRange = 5f;
    public float MaxAttackRange = 24f;

    [Header("Visual")]
    public Color Color = UnityEngine.Color.magenta;

    public EnemyDefinitionData ToRuntimeDefinition()
    {
        var fallback = EnemyDefinitionCatalog.Get(TypeId);
        var moveSpeed = MoveSpeed > 0f ? MoveSpeed : fallback.Movement.MoveSpeed;
        var bodyScale = BodyScale > 0f ? BodyScale : fallback.Stats.BodyScale;
        var minAttackRange = MinAttackRange > 0f ? MinAttackRange : fallback.Combat.MinAttackRange;
        var maxAttackRange = MaxAttackRange > 0f ? MaxAttackRange : fallback.Combat.MaxAttackRange;

        return new EnemyDefinitionData
        {
            TypeId = TypeId,
            Stats = new EnemyStatDefinition
            {
                MaxHealth = MaxHealth,
                HitRadius = HitRadius,
                BodyScale = bodyScale,
            },
            Movement = new EnemyMovementDefinition
            {
                Kind = Movement,
                MoveSpeed = moveSpeed,
            },
            Combat = new EnemyCombatDefinition
            {
                PrimaryAttack = PrimaryAttack,
                MinAttackRange = minAttackRange,
                MaxAttackRange = math.max(minAttackRange, maxAttackRange),
            },
            Visual = new EnemyVisualDefinition
            {
                Color = new float4(Color.r, Color.g, Color.b, Color.a),
            },
        };
    }
}
