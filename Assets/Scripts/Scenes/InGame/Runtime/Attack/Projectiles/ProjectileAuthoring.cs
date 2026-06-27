using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/// <summary>
/// Projectile プレハブを ECS 用に変換する Authoring。
/// 初期値は攻撃マスタから取り、実際の発射時に ProjectileSpawnSystem が上書きする。
/// </summary>
public class ProjectileAuthoring : MonoBehaviour
{
    public AttackDefinitionAsset DefaultAttackDefinition;
    public AttackDefinitionId DefaultAttack = AttackDefinitionId.BasicProjectile;

    class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var definition = authoring.DefaultAttackDefinition != null
                ? authoring.DefaultAttackDefinition.ToRuntimeDefinition()
                : AttackDefinitionCatalog.Get(authoring.DefaultAttack);

            AddComponent<ProjectileMotion>(entity);
            AddComponent(entity, new Projectile
            {
                Owner = Entity.Null,
                Team = TeamId.Neutral,
                Damage = definition.Damage,
                HitRadius = definition.HitRadius,
            });
            AddComponent(entity, new Hitbox { Radius = definition.HitRadius });
            AddComponent(entity, new Lifetime { Remaining = definition.Lifetime });
            AddComponent<SpatialHashTarget>(entity);
            AddComponent<GameplayActive>(entity);
            AddComponent<URPMaterialPropertyBaseColor>(entity);
        }
    }
}
