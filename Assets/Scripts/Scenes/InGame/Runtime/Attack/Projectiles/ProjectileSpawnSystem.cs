using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// ProjectileAttackRequest を消費して Projectile エンティティを生成または再利用するシステム。
/// PooledInstance がある弾は Destroy せず、GameplayActive を有効化して再初期化する。
/// </summary>
[UpdateAfter(typeof(AttackRequestSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileSpawnSystem : ISystem
{
    private EntityQuery requestQuery;
    private EntityQuery pooledProjectileQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<ProjectileAttackRequest>();

        requestQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ProjectileAttackRequest>()
            .Build(ref state);

        pooledProjectileQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Projectile, ProjectileMotion, Lifetime, LocalTransform, PooledInstance, GameplayActive>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var entityManager = state.EntityManager;
        var requestEntities = requestQuery.ToEntityArray(Allocator.Temp);
        var requests = requestQuery.ToComponentDataArray<ProjectileAttackRequest>(Allocator.Temp);
        var pooledProjectiles = pooledProjectileQuery.ToEntityArray(Allocator.Temp);

        for (var i = 0; i < requestEntities.Length; i++)
        {
            var value = requests[i];
            var projectileEntity = RentProjectile(entityManager, pooledProjectiles, config.ProjectilePrefab);

            InitializeProjectile(entityManager, projectileEntity, config.ProjectilePrefab, value);
            entityManager.DestroyEntity(requestEntities[i]);
        }

        pooledProjectiles.Dispose();
        requests.Dispose();
        requestEntities.Dispose();
    }

    private static Entity RentProjectile(
        EntityManager entityManager,
        NativeArray<Entity> pooledProjectiles,
        Entity projectilePrefab)
    {
        for (var i = 0; i < pooledProjectiles.Length; i++)
        {
            var candidate = pooledProjectiles[i];
            var pooledInstance = entityManager.GetComponentData<PooledInstance>(candidate);
            if (pooledInstance.SourcePrefab != projectilePrefab)
            {
                continue;
            }

            if (!entityManager.IsComponentEnabled<GameplayActive>(candidate))
            {
                entityManager.SetComponentEnabled<GameplayActive>(candidate, true);
                return candidate;
            }
        }

        var projectileEntity = entityManager.Instantiate(projectilePrefab);
        entityManager.AddComponentData(projectileEntity, new PooledInstance
        {
            SourcePrefab = projectilePrefab,
        });
        entityManager.SetComponentEnabled<GameplayActive>(projectileEntity, true);
        return projectileEntity;
    }

    private static void InitializeProjectile(
        EntityManager entityManager,
        Entity projectileEntity,
        Entity projectilePrefab,
        ProjectileAttackRequest value)
    {
        var transform = LocalTransform.FromPosition(value.Position);
        transform.Scale = value.Scale;
        entityManager.SetComponentData(projectileEntity, transform);

        if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(value.Owner))
        {
            var color = entityManager.GetComponentData<URPMaterialPropertyBaseColor>(value.Owner);
            entityManager.SetComponentData(projectileEntity, color);
        }

        entityManager.SetComponentData(projectileEntity, new ProjectileMotion
        {
            Shooter = value.Owner,
            Velocity = math.normalizesafe(value.Direction) * value.Speed,
        });
        entityManager.SetComponentData(projectileEntity, new Projectile
        {
            Owner = value.Owner,
            Team = value.Team,
            Damage = value.Damage,
            HitRadius = value.HitRadius,
        });
        entityManager.SetComponentData(projectileEntity, new Hitbox
        {
            Radius = value.HitRadius,
        });
        entityManager.SetComponentData(projectileEntity, new Lifetime
        {
            Remaining = value.Lifetime,
        });

        ApplyProjectileModifiers(entityManager, projectileEntity, value);

        if (!entityManager.HasComponent<PooledInstance>(projectileEntity))
        {
            entityManager.AddComponentData(projectileEntity, new PooledInstance
            {
                SourcePrefab = projectilePrefab,
            });
        }
    }

    private static void ApplyProjectileModifiers(
        EntityManager entityManager,
        Entity projectileEntity,
        ProjectileAttackRequest value)
    {
        SetTag<PiercingProjectile>(
            entityManager,
            projectileEntity,
            HasModifier(value.Modifiers, ProjectileModifierFlags.Piercing));
        SetTag<ChainingProjectile>(
            entityManager,
            projectileEntity,
            HasModifier(value.Modifiers, ProjectileModifierFlags.Chaining));
        SetTag<AreaOfEffectProjectile>(
            entityManager,
            projectileEntity,
            HasModifier(value.Modifiers, ProjectileModifierFlags.AreaOfEffect));

        var hasState = HasModifier(value.Modifiers, ProjectileModifierFlags.Piercing) ||
            HasModifier(value.Modifiers, ProjectileModifierFlags.Chaining) ||
            HasModifier(value.Modifiers, ProjectileModifierFlags.AreaOfEffect);

        if (hasState)
        {
            var state = new ProjectileModifierState
            {
                PierceRemaining = value.PierceCount,
                ChainRemaining = value.ChainCount,
                ImpactAreaRadius = value.ImpactAreaRadius,
            };

            if (entityManager.HasComponent<ProjectileModifierState>(projectileEntity))
            {
                entityManager.SetComponentData(projectileEntity, state);
            }
            else
            {
                entityManager.AddComponentData(projectileEntity, state);
            }
        }
        else if (entityManager.HasComponent<ProjectileModifierState>(projectileEntity))
        {
            entityManager.RemoveComponent<ProjectileModifierState>(projectileEntity);
        }
    }

    private static bool HasModifier(ProjectileModifierFlags value, ProjectileModifierFlags flag)
    {
        return (value & flag) != 0;
    }

    private static void SetTag<T>(
        EntityManager entityManager,
        Entity entity,
        bool enabled)
        where T : unmanaged, IComponentData
    {
        if (enabled)
        {
            if (!entityManager.HasComponent<T>(entity))
            {
                entityManager.AddComponent<T>(entity);
            }
        }
        else if (entityManager.HasComponent<T>(entity))
        {
            entityManager.RemoveComponent<T>(entity);
        }
    }
}
