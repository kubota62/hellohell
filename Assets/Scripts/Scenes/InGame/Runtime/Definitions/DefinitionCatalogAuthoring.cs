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

            if (attackBuffer.Length == 0)
            {
                attackBuffer.Add(AttackDefinitionElement.FromDefinition(
                    AttackDefinitionCatalog.Get(AttackDefinitionId.BasicProjectile)));
                attackBuffer.Add(AttackDefinitionElement.FromDefinition(
                    AttackDefinitionCatalog.Get(AttackDefinitionId.BasicAura)));
            }

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
    }
}
