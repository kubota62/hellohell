using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/// <summary>
/// Projectile プレハブを ECS 用に変換する Authoring。
/// 初期値は攻撃マスタから取り、Prefab 側でダメージ値を二重管理しない。
/// </summary>
public class ProjectileAuthoring : MonoBehaviour
{
    public AttackDefinitionId DefaultAttack = AttackDefinitionId.BasicProjectile;

    class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var definition = AttackDefinitionCatalog.GetProjectile(authoring.DefaultAttack);

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
