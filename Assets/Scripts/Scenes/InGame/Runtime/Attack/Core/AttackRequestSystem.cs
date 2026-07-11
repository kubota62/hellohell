using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Attack/Core の共通入口。
/// Player と Enemy の攻撃意思を読み、攻撃マスタを解決して、攻撃種別ごとの実行リクエストへ変換する。
/// Projectile / Aura / MeleeArc などの実処理は Attack/Variants 以下の専用Systemへ渡す。
/// </summary>
[UpdateBefore(typeof(ProjectileSpawnSystem))]
public partial struct AttackRequestSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var hasAttackMasters = SystemAPI.TryGetSingletonBuffer<AttackMasterElement>(
            out var attackMasters,
            true);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // 入力やAI判断を攻撃リクエストへ変換する。ここでは弾、範囲判定、エフェクトを直接生成しない。
        RequestPlayerAttack(ref state, ecb, hasAttackMasters, attackMasters, deltaTime);
        RequestEnemyAttack(ref state, ecb, hasAttackMasters, attackMasters, deltaTime);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void RequestPlayerAttack(
        ref SystemState state,
        EntityCommandBuffer ecb,
        bool hasAttackMasters,
        DynamicBuffer<AttackMasterElement> attackMasters,
        float deltaTime)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();

        foreach (var (_, cooldown, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>, RefRW<AttackCooldown>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            TryCreateAttackRequest(
                ref state,
                actorEntity,
                cooldown,
                ResolveAttackMasterId(ref state, actorEntity),
                input.IsFire,
                ecb,
                hasAttackMasters,
                attackMasters,
                deltaTime);
        }
    }

    private void RequestEnemyAttack(
        ref SystemState state,
        EntityCommandBuffer ecb,
        bool hasAttackMasters,
        DynamicBuffer<AttackMasterElement> attackMasters,
        float deltaTime)
    {
        var hasPlayer = SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
            SystemAPI.HasComponent<LocalTransform>(playerEntity);
        var playerPosition = hasPlayer
            ? SystemAPI.GetComponent<LocalTransform>(playerEntity).Position
            : float3.zero;

        foreach (var (_, cooldown, transform, attackRange, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>, RefRW<AttackCooldown>, RefRO<LocalTransform>, RefRO<EnemyAttackRange>>()
                     .WithAll<Enemy>()
                     .WithEntityAccess())
        {
            var attackMasterId = ResolveAttackMasterId(ref state, actorEntity);
            if (attackMasterId == AttackMasterId.None)
            {
                continue;
            }

            // 敵は定義された距離帯にPlayerがいると発射意思ありとして扱う。
            var distanceToPlayer = math.distance(transform.ValueRO.Position, playerPosition);
            var canFire = hasPlayer &&
                distanceToPlayer >= attackRange.ValueRO.Min &&
                distanceToPlayer <= attackRange.ValueRO.Max;

            TryCreateAttackRequest(
                ref state,
                actorEntity,
                cooldown,
                attackMasterId,
                canFire,
                ecb,
                hasAttackMasters,
                attackMasters,
                deltaTime);
        }
    }

    private void TryCreateAttackRequest(
        ref SystemState state,
        Entity actorEntity,
        RefRW<AttackCooldown> cooldown,
        AttackMasterId attackMasterId,
        bool wantsAttack,
        EntityCommandBuffer ecb,
        bool hasAttackMasters,
        DynamicBuffer<AttackMasterElement> attackMasters,
        float deltaTime)
    {
        // 攻撃IDからマスタ値を解決し、クールダウンが空いていればVariants向けのリクエストを発行する。
        var definition = hasAttackMasters
            ? AttackMasterCatalog.Get(attackMasters, attackMasterId)
            : AttackMasterCatalog.Get(attackMasterId);
        ApplyPlayerSkillStats(ref state, actorEntity, ref definition);

        cooldown.ValueRW.Remaining = math.max(0f, cooldown.ValueRO.Remaining - deltaTime);
        if (!wantsAttack || cooldown.ValueRO.Remaining > 0f)
        {
            return;
        }

        var actorBody = SystemAPI.GetComponent<ActorBody>(actorEntity);
        var actorTransform = SystemAPI.GetComponent<LocalTransform>(actorEntity);
        var canonLtw = SystemAPI.GetComponent<LocalToWorld>(actorBody.Canon);
        var team = TeamId.Neutral;
        if (SystemAPI.HasComponent<Team>(actorEntity))
        {
            team = SystemAPI.GetComponent<Team>(actorEntity).Value;
        }

        switch (definition.Kind)
        {
            case AttackKind.Aura:
                CreateAuraRequest(actorEntity, team, canonLtw.Position, definition, ecb);
                break;

            case AttackKind.Projectile:
                CreateProjectileRequest(actorEntity, team, canonLtw, definition, ecb);
                break;

            case AttackKind.MeleeArc:
                CreateMeleeArcRequest(actorEntity, team, actorTransform.Position, canonLtw, definition, ecb);
                break;

            case AttackKind.Beam:
            default:
                return;
        }

        cooldown.ValueRW.Remaining = math.max(0.01f, definition.Cooldown);
    }

    private static void ApplyPlayerSkillStats(
        ref SystemState state,
        Entity actorEntity,
        ref AttackMasterData definition)
    {
        if (!state.EntityManager.HasComponent<PlayerSkillStats>(actorEntity))
        {
            return;
        }

        var stats = state.EntityManager.GetComponentData<PlayerSkillStats>(actorEntity);
        definition.Damage = math.max(1, (int)math.round(definition.Damage * PlayerAutoSkillSystem.GetDamageMultiplier(stats)));
        definition.Cooldown *= PlayerAutoSkillSystem.GetCooldownMultiplier(stats);
    }

    private void CreateProjectileRequest(
        Entity actorEntity,
        TeamId team,
        LocalToWorld canonLtw,
        AttackMasterData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new ProjectileAttackRequest
        {
            AttackMasterId = definition.Id,
            Owner = actorEntity,
            Team = team,
            Position = canonLtw.Position,
            Direction = GetAttackDirection(canonLtw),
            Speed = definition.ProjectileSpeed,
            Damage = definition.Damage,
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

    private void CreateAuraRequest(
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
        });
    }

    private void CreateMeleeArcRequest(
        Entity actorEntity,
        TeamId team,
        float3 actorPosition,
        LocalToWorld canonLtw,
        AttackMasterData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new MeleeArcAttackRequest
        {
            AttackMasterId = definition.Id,
            Owner = actorEntity,
            Team = team,
            Position = actorPosition,
            Direction = GetAttackDirection(canonLtw),
            Radius = definition.AreaRadius,
            AngleDegrees = definition.ArcAngleDegrees,
            Damage = definition.Damage,
            VisualDuration = definition.VisualDuration,
        });
    }

    private AttackMasterId ResolveAttackMasterId(ref SystemState state, Entity actorEntity)
    {
        if (!SystemAPI.HasComponent<AttackLoadout>(actorEntity))
        {
            return AttackMasterId.BasicProjectile;
        }

        return SystemAPI.GetComponent<AttackLoadout>(actorEntity).PrimaryAttack;
    }

    private static float3 GetAttackDirection(LocalToWorld canonLtw)
    {
        var direction = canonLtw.Up;
        direction.y = 0f;
        return math.normalizesafe(direction, new float3(0f, 0f, 1f));
    }
}
