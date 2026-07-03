using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 敵の種類、移動、初期ステータス、攻撃をまとめて管理する ScriptableObject。
/// 将来はこの定義を Baker で BlobAsset 化し、スポーン時に参照する。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Definitions/Enemy Definition")]
public class EnemyDefinitionAsset : ScriptableObject
{
    public int TypeId = 1;
    public EnemyMovementKind Movement = EnemyMovementKind.Forward;
    public int MaxHealth = 100;
    public float MoveSpeed = 2.8f;
    public float BodyScale = 1f;
    public float HitRadius = 2f;
    public float MinAttackRange = 5f;
    public float MaxAttackRange = 24f;
    public AttackDefinitionId PrimaryAttack = AttackDefinitionId.BasicProjectile;
    public Color Color = UnityEngine.Color.magenta;

    public EnemyDefinitionData ToRuntimeDefinition()
    {
        var fallback = EnemyDefinitionCatalog.Get(TypeId);
        var moveSpeed = MoveSpeed > 0f ? MoveSpeed : fallback.MoveSpeed;
        var bodyScale = BodyScale > 0f ? BodyScale : fallback.BodyScale;
        var minAttackRange = MinAttackRange > 0f ? MinAttackRange : fallback.MinAttackRange;
        var maxAttackRange = MaxAttackRange > 0f ? MaxAttackRange : fallback.MaxAttackRange;

        return new EnemyDefinitionData
        {
            TypeId = TypeId,
            Movement = Movement,
            MaxHealth = MaxHealth,
            MoveSpeed = moveSpeed,
            BodyScale = bodyScale,
            HitRadius = HitRadius,
            MinAttackRange = minAttackRange,
            MaxAttackRange = math.max(minAttackRange, maxAttackRange),
            PrimaryAttack = PrimaryAttack,
            Color = new float4(Color.r, Color.g, Color.b, Color.a),
        };
    }
}
