using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Player と Enemy の発射条件を見て、Projectile 攻撃リクエストを作るシステム。
/// 攻撃値は AttackLoadout の定義IDから取得し、Projectile の生成は ProjectileSpawnSystem に任せる。
/// </summary>
[UpdateBefore(typeof(ProjectileSpawnSystem))]
public partial struct ProjectileAttackRequestSystem : ISystem
{
    private static readonly float ShootInterval = 1.0f;

    private float timer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0) return;
        timer = ShootInterval;

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        RequestPlayerAttack(ref state, ecb);
        RequestEnemyAttack(ref state, ecb);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void RequestPlayerAttack(
        ref SystemState state,
        EntityCommandBuffer ecb)
    {
        var input = SystemAPI.GetSingleton<PlayerInput>();
        if (!input.IsFire) return;

        foreach (var (_, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            CreateProjectileRequest(ref state, actorEntity, ResolveAttackDefinitionId(ref state, actorEntity), ecb);
        }
    }

    private void RequestEnemyAttack(
        ref SystemState state,
        EntityCommandBuffer ecb)
    {
        foreach (var (_, actorEntity) in
                 SystemAPI.Query<RefRO<ActorBody>>()
                     .WithAll<Enemy>()
                     .WithEntityAccess())
        {
            CreateProjectileRequest(ref state, actorEntity, ResolveAttackDefinitionId(ref state, actorEntity), ecb);
        }
    }

    private void CreateProjectileRequest(
        ref SystemState state,
        Entity actorEntity,
        AttackDefinitionId attackDefinitionId,
        EntityCommandBuffer ecb)
    {
        var actorBody = SystemAPI.GetComponent<ActorBody>(actorEntity);
        var canonLtw = SystemAPI.GetComponent<LocalToWorld>(actorBody.Canon);
        var definition = AttackDefinitionCatalog.GetProjectile(attackDefinitionId);

        var team = TeamId.Neutral;
        if (SystemAPI.HasComponent<Team>(actorEntity))
        {
            team = SystemAPI.GetComponent<Team>(actorEntity).Value;
        }

        var requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new ProjectileAttackRequest
        {
            AttackDefinitionId = definition.Id,
            Owner = actorEntity,
            Team = team,
            Position = canonLtw.Position,
            Direction = math.normalizesafe(canonLtw.Up, new float3(0f, 0f, 1f)),
            Speed = definition.Speed,
            Damage = definition.Damage,
            HitRadius = definition.HitRadius,
            Lifetime = definition.Lifetime,
            Scale = definition.Scale,
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
