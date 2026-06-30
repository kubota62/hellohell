using Unity.Entities;
using UnityEngine;

/// <summary>
/// ScriptableObject の定義一覧を ECS の定義バッファへ焼き込む Authoring。
/// シーンに置くと、スポーンや攻撃生成は静的カタログではなくこの定義を優先して読む。
/// </summary>
public class DefinitionCatalogAuthoring : MonoBehaviour
{
    public AttackDefinitionAsset[] AttackDefinitions;
    public EnemyDefinitionAsset[] EnemyDefinitions;

    class Baker : Baker<DefinitionCatalogAuthoring>
    {
        public override void Bake(DefinitionCatalogAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent<DefinitionCatalogTag>(entity);

            var attackBuffer = AddBuffer<AttackDefinitionElement>(entity);
            if (authoring.AttackDefinitions is { Length: > 0 })
            {
                foreach (var asset in authoring.AttackDefinitions)
                {
                    if (asset == null) continue;
                    attackBuffer.Add(AttackDefinitionElement.FromDefinition(asset.ToRuntimeDefinition()));
                }
            }

            EnsureAttackDefinition(attackBuffer, AttackDefinitionId.BasicProjectile);
            EnsureAttackDefinition(attackBuffer, AttackDefinitionId.BasicAura);
            EnsureAttackDefinition(attackBuffer, AttackDefinitionId.BasicMeleeArc);

            var enemyBuffer = AddBuffer<EnemyDefinitionElement>(entity);
            if (authoring.EnemyDefinitions is { Length: > 0 })
            {
                foreach (var asset in authoring.EnemyDefinitions)
                {
                    if (asset == null) continue;
                    enemyBuffer.Add(EnemyDefinitionElement.FromDefinition(asset.ToRuntimeDefinition()));
                }
            }

            if (enemyBuffer.Length == 0)
            {
                enemyBuffer.Add(EnemyDefinitionElement.FromDefinition(
                    EnemyDefinitionCatalog.Get(EnemyDefinitionCatalog.ForwardEnemy)));
                enemyBuffer.Add(EnemyDefinitionElement.FromDefinition(
                    EnemyDefinitionCatalog.Get(EnemyDefinitionCatalog.RandomEnemy)));
            }
        }

        private static void EnsureAttackDefinition(
            DynamicBuffer<AttackDefinitionElement> attackBuffer,
            AttackDefinitionId id)
        {
            for (var i = 0; i < attackBuffer.Length; i++)
            {
                if (attackBuffer[i].Id == id)
                {
                    return;
                }
            }

            attackBuffer.Add(AttackDefinitionElement.FromDefinition(AttackDefinitionCatalog.Get(id)));
        }
    }
}
