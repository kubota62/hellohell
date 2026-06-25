using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Player と Enemy の攻撃入力を見て、攻撃種別ごとのリクエストを作るシステム。
/// 攻撃マスタをここで解決し、Projectile や Aura などの実行システムへ薄いリクエストとして渡す。
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
        foreach (var (_, cooldown, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>, RefRW<AttackCooldown>>()
                     .WithAll<Enemy>()
                     .WithEntityAccess())
        {
            TryCreateAttackRequest(
                ref state,
                actorEntity,
                cooldown,
                ResolveAttackDefinitionId(ref state, actorEntity),
                true,
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
        var definition = hasAttackDefinitions
            ? AttackDefinitionCatalog.Get(attackDefinitions, attackDefinitionId)
            : AttackDefinitionCatalog.Get(attackDefinitionId);

        cooldown.ValueRW.Remaining = math.max(0f, cooldown.ValueRO.Remaining - deltaTime);
        if (!wantsAttack || cooldown.ValueRO.Remaining > 0f)
        {
            return;
        }

        var actorBody = SystemAPI.GetComponent<ActorBody>(actorEntity);
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
            Direction = math.normalizesafe(canonLtw.Up, new float3(0f, 0f, 1f)),
            Speed = definition.ProjectileSpeed,
            Damage = definition.Damage,
            HitRadius = definition.HitRadius,
            Lifetime = definition.Lifetime,
            Scale = definition.Scale,
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

    private AttackDefinitionId ResolveAttackDefinitionId(ref SystemState state, Entity actorEntity)
    {
        if (!SystemAPI.HasComponent<AttackLoadout>(actorEntity))
        {
            return AttackDefinitionId.BasicProjectile;
        }

        return SystemAPI.GetComponent<AttackLoadout>(actorEntity).PrimaryAttack;
    }
}
