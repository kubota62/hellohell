using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// PlayerとEnemyの攻撃意思を、攻撃方式ごとのリクエストへ変換する。
/// </summary>
[UpdateBefore(typeof(ProjectileSpawnSystem))]
public partial struct AttackRequestSystem : ISystem
{
    private Unity.Mathematics.Random criticalRandom;

    public void OnCreate(ref SystemState state)
    {
        criticalRandom = new Unity.Mathematics.Random(0xC17C0DEu);
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PlayerInput>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

        var hasAttackMasters = SystemAPI.TryGetSingletonBuffer<AttackMasterElement>(
            out var attackMasters,
            true);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var deltaTime = SystemAPI.Time.DeltaTime;

        RequestPlayerAttacks(
            ref state,
            ecb,
            hasAttackMasters,
            attackMasters,
            deltaTime);
        RequestEnemyAttacks(
            ref state,
            ecb,
            hasAttackMasters,
            attackMasters,
            deltaTime);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void RequestPlayerAttacks(
        ref SystemState state,
        EntityCommandBuffer ecb,
        bool hasAttackMasters,
        DynamicBuffer<AttackMasterElement> attackMasters,
        float deltaTime)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();
        var entityManager = state.EntityManager;

        foreach (var (_, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>>()
                     .WithAll<Player, PlayerAttackSlot>()
                     .WithNone<PlayerDefeated>()
                     .WithEntityAccess())
        {
            var attackSlots = entityManager.GetBuffer<PlayerAttackSlot>(actorEntity);
            for (var i = 0; i < attackSlots.Length; i++)
            {
                var slot = attackSlots[i];
                slot.Remaining = TickCooldown(slot.Remaining, deltaTime);

                if (!input.IsFire ||
                    !input.ActiveAttackMask.Contains(slot.AttackMasterId) ||
                    slot.Remaining > 0f)
                {
                    attackSlots[i] = slot;
                    continue;
                }

                if (!CanUsePlayerAttack(
                        entityManager,
                        actorEntity,
                        slot.AttackMasterId))
                {
                    attackSlots[i] = slot;
                    continue;
                }

                var definition = ResolveDefinition(
                    hasAttackMasters,
                    attackMasters,
                    slot.AttackMasterId);
                var attackCount = ApplyPlayerSkillStats(
                    entityManager,
                    actorEntity,
                    ref definition,
                    criticalRandom.NextUInt(),
                    criticalRandom.NextUInt());

                var createdAttack = false;
                for (var attackIndex = 0; attackIndex < attackCount; attackIndex++)
                {
                    createdAttack |= CreateAttackRequest(
                        entityManager,
                        actorEntity,
                        definition,
                        false,
                        float3.zero,
                        ecb);
                }

                if (createdAttack)
                {
                    slot.Remaining = math.max(0.01f, definition.Cooldown);
                }

                attackSlots[i] = slot;
            }
        }
    }

    private void RequestEnemyAttacks(
        ref SystemState state,
        EntityCommandBuffer ecb,
        bool hasAttackMasters,
        DynamicBuffer<AttackMasterElement> attackMasters,
        float deltaTime)
    {
        var entityManager = state.EntityManager;
        var hasPlayer = SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity);
        var playerPosition = hasPlayer
            ? SystemAPI.GetComponent<LocalTransform>(playerEntity).Position
            : float3.zero;

        foreach (var (cooldown, transform, attackRange, actorEntity) in
                 SystemAPI.Query<RefRW<AttackCooldown>, RefRO<LocalTransform>, RefRO<EnemyAttackRange>>()
                     .WithAll<Enemy>()
                     .WithEntityAccess())
        {
            cooldown.ValueRW.Remaining = TickCooldown(
                cooldown.ValueRO.Remaining,
                deltaTime);

            if (!hasPlayer || cooldown.ValueRO.Remaining > 0f)
            {
                continue;
            }

            var distanceToPlayer = math.distance(
                transform.ValueRO.Position,
                playerPosition);
            if (distanceToPlayer < attackRange.ValueRO.Min ||
                distanceToPlayer > attackRange.ValueRO.Max)
            {
                continue;
            }

            var attackMasterId = ResolveActorAttack(entityManager, actorEntity);
            if (attackMasterId == AttackMasterId.None)
            {
                continue;
            }

            var definition = ResolveDefinition(
                hasAttackMasters,
                attackMasters,
                attackMasterId);
            ApplyEnemyModifiers(entityManager, actorEntity, ref definition);
            if (CreateAttackRequest(
                    entityManager,
                    actorEntity,
                    definition,
                    true,
                    playerPosition - transform.ValueRO.Position,
                    ecb))
            {
                cooldown.ValueRW.Remaining = math.max(0.01f, definition.Cooldown);
            }
        }
    }

    private static AttackMasterData ResolveDefinition(
        bool hasAttackMasters,
        DynamicBuffer<AttackMasterElement> attackMasters,
        AttackMasterId attackMasterId)
    {
        return hasAttackMasters
            ? AttackMasterCatalog.Get(attackMasters, attackMasterId)
            : AttackMasterCatalog.Get(attackMasterId);
    }

    private static int ApplyPlayerSkillStats(
        EntityManager entityManager,
        Entity actorEntity,
        ref AttackMasterData definition,
        uint criticalRoll,
        uint multistrikeRoll)
    {
        if (!entityManager.HasComponent<PlayerSkillStats>(actorEntity))
        {
            return 1;
        }

        var stats = entityManager.GetComponentData<PlayerSkillStats>(actorEntity);
        definition.Damage = math.max(
            1,
            (int)math.round(
                definition.Damage *
                PlayerAutoSkillSystem.GetDamageMultiplier(stats) *
                PlayerAutoSkillSystem.GetWeaponDamageMultiplier(
                    stats,
                    definition.Id)));
        definition.Cooldown *= PlayerAutoSkillSystem.GetCooldownMultiplier(stats);
        var areaMultiplier = PlayerAutoSkillSystem.GetAreaMultiplier(stats);
        definition.HitRadius *= areaMultiplier;
        definition.AreaRadius *= areaMultiplier;
        definition.ImpactAreaRadius *= areaMultiplier;
        definition.Scale *= areaMultiplier;
        if (definition.Kind == AttackKind.Projectile)
        {
            definition.Lifetime = ResolveProjectileLifetime(
                definition.Lifetime,
                stats);
            definition.PierceCount = ResolveProjectilePierceCount(
                definition.PierceCount,
                stats);
            definition.ProjectileModifiers = ResolveProjectileModifiers(
                definition.ProjectileModifiers,
                definition.PierceCount);
        }
        definition.Damage = ResolveCriticalDamage(
            definition.Damage,
            stats,
            criticalRoll,
            out var isCritical);
        definition.IsCritical = isCritical ? (byte)1 : (byte)0;
        return ResolveAttackCount(stats, multistrikeRoll);
    }

    public static float ResolveProjectileLifetime(
        float baseLifetime,
        in PlayerSkillStats stats)
    {
        return math.max(
            0.05f,
            baseLifetime *
            PlayerAutoSkillSystem.GetProjectileLifetimeMultiplier(stats));
    }

    public static int ResolveProjectilePierceCount(
        int basePierceCount,
        in PlayerSkillStats stats)
    {
        return math.max(
            0,
            basePierceCount) +
            PlayerAutoSkillSystem.GetProjectilePierceAdd(stats);
    }

    public static ProjectileModifierFlags ResolveProjectileModifiers(
        ProjectileModifierFlags baseModifiers,
        int pierceCount)
    {
        return pierceCount > 0
            ? baseModifiers | ProjectileModifierFlags.Piercing
            : baseModifiers;
    }

    public static int ResolveAttackCount(
        in PlayerSkillStats stats,
        uint roll)
    {
        var chance = PlayerAutoSkillSystem.GetMultistrikeChance(stats);
        return chance > 0f &&
            roll / (float)uint.MaxValue < chance
                ? 2
                : 1;
    }

    public static int ResolveCriticalDamage(
        int damage,
        in PlayerSkillStats stats,
        uint roll,
        out bool isCritical)
    {
        var chance = PlayerAutoSkillSystem.GetCriticalChance(stats);
        isCritical = chance > 0f &&
            roll / (float)uint.MaxValue < chance;
        if (!isCritical)
        {
            return math.max(1, damage);
        }

        return math.max(
            1,
            (int)math.round(
                damage *
                PlayerAutoSkillSystem.GetCriticalDamageMultiplier(stats)));
    }

    private static bool CanUsePlayerAttack(
        EntityManager entityManager,
        Entity actorEntity,
        AttackMasterId attackMasterId)
    {
        if (!entityManager.HasComponent<PlayerSkillStats>(actorEntity))
        {
            return true;
        }

        var stats = entityManager.GetComponentData<PlayerSkillStats>(actorEntity);
        return PlayerAutoSkillSystem.GetWeaponLevel(stats, attackMasterId) > 0;
    }

    private static void ApplyEnemyModifiers(
        EntityManager entityManager,
        Entity actorEntity,
        ref AttackMasterData definition)
    {
        if (entityManager.HasComponent<FinalBossEnemy>(actorEntity))
        {
            var isEnraged =
                entityManager.HasComponent<FinalBossPhaseState>(actorEntity) &&
                entityManager.GetComponentData<FinalBossPhaseState>(
                    actorEntity).IsEnraged != 0;
            ApplyFinalBossAttackModifiers(
                ref definition,
                isEnraged);
            return;
        }

        if (entityManager.HasComponent<ChampionEnemy>(actorEntity))
        {
            definition.Damage = math.max(1, definition.Damage * 3);
            definition.Cooldown = math.max(0.05f, definition.Cooldown * 0.65f);
            definition.HitRadius *= 1.5f;
            definition.AreaRadius *= 1.5f;
            definition.ImpactAreaRadius *= 1.5f;
            definition.Scale *= 1.5f;
            return;
        }

        if (!entityManager.HasComponent<EliteEnemy>(actorEntity))
        {
            return;
        }

        definition.Damage = math.max(1, definition.Damage * 2);
        definition.Cooldown = math.max(0.05f, definition.Cooldown * 0.8f);
        definition.HitRadius *= 1.2f;
        definition.AreaRadius *= 1.2f;
        definition.ImpactAreaRadius *= 1.2f;
        definition.Scale *= 1.2f;
    }

    public static void ApplyFinalBossAttackModifiers(
        ref AttackMasterData definition,
        bool isEnraged)
    {
        var damageMultiplier = isEnraged ? 7 : 5;
        var cooldownMultiplier = isEnraged ? 0.35f : 0.5f;
        var areaMultiplier = isEnraged ? 2.25f : 2f;
        definition.Damage = math.max(
            1,
            definition.Damage * damageMultiplier);
        definition.Cooldown = math.max(
            0.05f,
            definition.Cooldown * cooldownMultiplier);
        definition.HitRadius *= areaMultiplier;
        definition.AreaRadius *= areaMultiplier;
        definition.ImpactAreaRadius *= areaMultiplier;
        definition.Scale *= areaMultiplier;
    }

    private static bool CreateAttackRequest(
        EntityManager entityManager,
        Entity actorEntity,
        AttackMasterData definition,
        bool useDirectionOverride,
        float3 directionOverride,
        EntityCommandBuffer ecb)
    {
        var actorBody = entityManager.GetComponentData<ActorBody>(actorEntity);
        var actorTransform = entityManager.GetComponentData<LocalTransform>(actorEntity);
        var canonTransform = entityManager.GetComponentData<LocalToWorld>(actorBody.Canon);
        var direction = useDirectionOverride
            ? FlattenDirection(directionOverride)
            : FlattenDirection(canonTransform.Up);
        var team = entityManager.HasComponent<Team>(actorEntity)
            ? entityManager.GetComponentData<Team>(actorEntity).Value
            : TeamId.Neutral;

        switch (definition.Kind)
        {
            case AttackKind.Projectile:
                CreateProjectileRequest(
                    actorEntity,
                    team,
                    canonTransform.Position,
                    direction,
                    definition,
                    ecb);
                return true;

            case AttackKind.Aura:
                CreateAuraRequest(
                    actorEntity,
                    team,
                    canonTransform.Position,
                    definition,
                    ecb);
                return true;

            case AttackKind.MeleeArc:
                CreateMeleeArcRequest(
                    actorEntity,
                    team,
                    actorTransform.Position,
                    direction,
                    definition,
                    ecb);
                return true;

            case AttackKind.Beam:
            default:
                return false;
        }
    }

    private static void CreateProjectileRequest(
        Entity actorEntity,
        TeamId team,
        float3 position,
        float3 direction,
        AttackMasterData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new ProjectileAttackRequest
        {
            AttackMasterId = definition.Id,
            Owner = actorEntity,
            Team = team,
            Position = position,
            Direction = direction,
            Speed = definition.ProjectileSpeed,
            Damage = definition.Damage,
            IsCritical = definition.IsCritical,
            HitRadius = definition.HitRadius,
            Lifetime = definition.Lifetime,
            Scale = definition.Scale,
            Modifiers = definition.ProjectileModifiers,
            PierceCount = definition.PierceCount,
            ChainCount = definition.ChainCount,
            ChainRange = definition.ChainRange,
            ImpactAreaRadius = definition.ImpactAreaRadius,
        });
    }

    private static void CreateAuraRequest(
        Entity actorEntity,
        TeamId team,
        float3 position,
        AttackMasterData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new AuraAttackRequest
        {
            AttackMasterId = definition.Id,
            Owner = actorEntity,
            Team = team,
            Position = position,
            Radius = definition.AreaRadius,
            Damage = definition.Damage,
            IsCritical = definition.IsCritical,
        });
    }

    private static void CreateMeleeArcRequest(
        Entity actorEntity,
        TeamId team,
        float3 position,
        float3 direction,
        AttackMasterData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new MeleeArcAttackRequest
        {
            AttackMasterId = definition.Id,
            Owner = actorEntity,
            Team = team,
            Position = position,
            Direction = direction,
            Radius = definition.AreaRadius,
            AngleDegrees = definition.ArcAngleDegrees,
            Damage = definition.Damage,
            IsCritical = definition.IsCritical,
            VisualDuration = definition.VisualDuration,
        });
    }

    private static AttackMasterId ResolveActorAttack(
        EntityManager entityManager,
        Entity actorEntity)
    {
        return entityManager.HasComponent<AttackLoadout>(actorEntity)
            ? entityManager.GetComponentData<AttackLoadout>(actorEntity).PrimaryAttack
            : AttackMasterId.BasicProjectile;
    }

    private static float TickCooldown(float remaining, float deltaTime)
    {
        return math.max(0f, remaining - deltaTime);
    }

    private static float3 FlattenDirection(float3 direction)
    {
        direction.y = 0f;
        return math.normalizesafe(direction, new float3(0f, 0f, 1f));
    }
}
