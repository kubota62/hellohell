using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public int DefaultDamage = 34;
    public float HitRadius = 0.5f;
    public float LifetimeSeconds = 5f;

    class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent<ProjectileMotion>(entity);
            AddComponent(entity, new Projectile
            {
                Owner = Entity.Null,
                Team = TeamId.Neutral,
                Damage = authoring.DefaultDamage,
                HitRadius = authoring.HitRadius,
            });
            AddComponent(entity, new Hitbox { Radius = authoring.HitRadius });
            AddComponent(entity, new Lifetime { Remaining = authoring.LifetimeSeconds });
            AddComponent<SpatialHashTarget>(entity);
            AddComponent<GameplayActive>(entity);
            AddComponent<URPMaterialPropertyBaseColor>(entity);
        }
    }
}
