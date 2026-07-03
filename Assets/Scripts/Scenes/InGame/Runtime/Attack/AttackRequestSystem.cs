using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Player と Enemy の攻撃意思を読み、攻撃種別ごとの実行リクエストへ変換するシステム。
/// 攻撃そのものはここで実行せず、Projectile / Aura / MeleeArc などの専用Systemへ薄いデータとして渡す。
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
        var hasAttackDefinitions = SystemAPI.TryGetSingletonBuffer<AttackDefinitionElement>(
            out var attackDefinitions,
            true);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // 入力やAI判断を攻撃リクエストへ変換する。ここでは弾やエフェクトを直接生成しない。
        RequestPlayerAttack(ref state, ecb, hasAttackDefinitions, attackDefinitions, deltaTime);
        RequestEnemyAttack(ref state, ecb, hasAttackDefinitions, attackDefinitions, deltaTime);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void RequestPlayerAttack(
        ref SystemState state,
        EntityCommandBuffer ecb,
        bool hasAttackDefinitions,
        DynamicBuffer<AttackDefinitionElement> attackDefinitions,
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
                ResolveAttackDefinitionId(ref state, actorEntity),
                input.IsFire,
                ecb,
                hasAttackDefinitions,
                attackDefinitions,
                deltaTime);
        }
    }

    private void RequestEnemyAttack(
        ref SystemState state,
        EntityCommandBuffer ecb,
        bool hasAttackDefinitions,
        DynamicBuffer<AttackDefinitionElement> attackDefinitions,
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
            // 敵は定義された距離帯にPlayerがいると発射意思ありとして扱う。
            var distanceToPlayer = math.distance(transform.ValueRO.Position, playerPosition);
            var canFire = hasPlayer &&
                distanceToPlayer >= attackRange.ValueRO.Min &&
                distanceToPlayer <= attackRange.ValueRO.Max;

            TryCreateAttackRequest(
                ref state,
                actorEntity,
                cooldown,
                ResolveAttackDefinitionId(ref state, actorEntity),
                canFire,
                ecb,
                hasAttackDefinitions,
                attackDefinitions,
                deltaTime);
        }
    }

    private void TryCreateAttackRequest(
        ref SystemState state,
        Entity actorEntity,
        RefRW<AttackCooldown> cooldown,
        AttackDefinitionId attackDefinitionId,
        bool wantsAttack,
        EntityCommandBuffer ecb,
        bool hasAttackDefinitions,
        DynamicBuffer<AttackDefinitionElement> attackDefinitions,
        float deltaTime)
    {
        // 攻撃IDからマスタ値を解決し、クールダウンが空いていれば種別ごとのリクエストを発行する。
        var definition = hasAttackDefinitions
            ? AttackDefinitionCatalog.Get(attackDefinitions, attackDefinitionId)
            : AttackDefinitionCatalog.Get(attackDefinitionId);

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

    private void CreateProjectileRequest(
        Entity actorEntity,
        TeamId team,
        LocalToWorld canonLtw,
        AttackDefinitionData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new ProjectileAttackRequest
        {
            AttackDefinitionId = definition.Id,
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
        AttackDefinitionData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new AuraAttackRequest
        {
            AttackDefinitionId = definition.Id,
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
        AttackDefinitionData definition,
        EntityCommandBuffer ecb)
    {
        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new MeleeArcAttackRequest
        {
            AttackDefinitionId = definition.Id,
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

    private AttackDefinitionId ResolveAttackDefinitionId(ref SystemState state, Entity actorEntity)
    {
        if (!SystemAPI.HasComponent<AttackLoadout>(actorEntity))
        {
            return AttackDefinitionId.BasicProjectile;
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
