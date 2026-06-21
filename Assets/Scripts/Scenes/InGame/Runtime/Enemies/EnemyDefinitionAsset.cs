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
    public float HitRadius = 2f;
    public AttackDefinitionId PrimaryAttack = AttackDefinitionId.BasicProjectile;
    public Color Color = UnityEngine.Color.magenta;

    public EnemyDefinitionData ToRuntimeDefinition()
    {
        return new EnemyDefinitionData
        {
            TypeId = TypeId,
            Movement = Movement,
            MaxHealth = MaxHealth,
            HitRadius = HitRadius,
            PrimaryAttack = PrimaryAttack,
            Color = new float4(Color.r, Color.g, Color.b, Color.a),
        };
    }
}
