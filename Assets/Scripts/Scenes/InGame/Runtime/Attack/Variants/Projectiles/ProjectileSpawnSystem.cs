using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// Attack/Variants/Projectiles の生成処理。
/// ProjectileAttackRequest を消費してProjectileエンティティを生成、またはプールから再利用する。
/// PooledInstance を持つ弾はDestroyせず、GameplayActiveを切り替えて再初期化する。
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
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var config = SystemAPI.GetSingleton<Config>();
        var entityManager = state.EntityManager;
        var requestEntities = requestQuery.ToEntityArray(Allocator.Temp);
        var requests = requestQuery.ToComponentDataArray<ProjectileAttackRequest>(Allocator.Temp);
        var pooledProjectiles = pooledProjectileQuery.ToEntityArray(Allocator.Temp);

        // リクエスト単位で弾を借り、位置、速度、寿命、Modifier状態をまとめて初期化する。
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
        // 同じPrefab由来で非アクティブな弾があれば再利用する。なければ初回だけInstantiateする。
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
            IsCritical = value.IsCritical,
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
        EnsureProjectileRuntimeBuffers(entityManager, projectileEntity);

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
        // Modifierはタグと状態値を分けて持たせる。検索はタグ、残り回数や範囲は状態値で読む。
        SetTag<PiercingProjectile>(
            entityManager,
            projectileEntity,
            value.Modifiers.Has(ProjectileModifierFlags.Piercing));
        SetTag<ChainingProjectile>(
            entityManager,
            projectileEntity,
            value.Modifiers.Has(ProjectileModifierFlags.Chaining));
        SetTag<AreaOfEffectProjectile>(
            entityManager,
            projectileEntity,
            value.Modifiers.Has(ProjectileModifierFlags.AreaOfEffect));

        var hasState = value.Modifiers.Has(ProjectileModifierFlags.Piercing) ||
            value.Modifiers.Has(ProjectileModifierFlags.Chaining) ||
            value.Modifiers.Has(ProjectileModifierFlags.AreaOfEffect);

        var state = new ProjectileModifierState
        {
            Modifiers = value.Modifiers,
            PierceRemaining = hasState ? value.PierceCount : 0,
            ChainRemaining = hasState ? value.ChainCount : 0,
            ChainRange = hasState ? value.ChainRange : 0f,
            ImpactAreaRadius = hasState ? value.ImpactAreaRadius : 0f,
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

    private static void EnsureProjectileRuntimeBuffers(
        EntityManager entityManager,
        Entity projectileEntity)
    {
        if (!entityManager.HasComponent<ProjectileModifierState>(projectileEntity))
        {
            entityManager.AddComponentData(projectileEntity, new ProjectileModifierState());
        }

        if (!entityManager.HasBuffer<ProjectileHitRecord>(projectileEntity))
        {
            entityManager.AddBuffer<ProjectileHitRecord>(projectileEntity);
        }

        entityManager.GetBuffer<ProjectileHitRecord>(projectileEntity).Clear();
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
