using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 現在生存している ActorBody、active な Projectile、PlayerProgressを集計し、Managed 側の HUD へ渡す。
/// inactive なプール待機弾は Projectile 数に含めない。
/// </summary>
[BurstCompile]
public partial struct HUDSystem : ISystem
{
    EntityQuery actorCountQuery;
    EntityQuery projectileCountQuery;
    EntityQuery eliteCountQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        actorCountQuery = SystemAPI.QueryBuilder().WithAll<ActorBody>().Build();
        projectileCountQuery = SystemAPI.QueryBuilder().WithAll<Projectile, GameplayActive>().Build();
        eliteCountQuery = SystemAPI.QueryBuilder().WithAll<EliteEnemy, GameplayActive>().Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        var hudBridge = HUDBridge.Instance;
        if (hudBridge == null)
        {
            return;
        }

        hudBridge.SetActorCount(actorCountQuery.CalculateEntityCount());
        hudBridge.SetProjectileCount(projectileCountQuery.CalculateEntityCount());
        hudBridge.SetEliteCount(eliteCountQuery.CalculateEntityCount());

        if (SystemAPI.TryGetSingleton<PlayerProgress>(out var progress))
        {
            var health = new Health { Max = 1, Current = 1 };
            if (SystemAPI.TryGetSingletonEntity<Player>(out var playerEntity) &&
                SystemAPI.HasComponent<Health>(playerEntity))
            {
                health = SystemAPI.GetComponent<Health>(playerEntity);
            }

            var runState = SystemAPI.TryGetSingleton<RunState>(out var currentRun)
                ? currentRun
                : new RunState { ThreatLevel = 1 };
            var autoAttackEnabled =
                SystemAPI.TryGetSingleton<PlayerInput>(out var input) &&
                input.AutoAttackEnabled;

            hudBridge.SetPlayerProgress(
                progress.Level,
                progress.Experience,
                progress.ExperienceToNextLevel,
                progress.Score,
                health.Current,
                health.Max,
                runState.ElapsedSeconds,
                runState.ThreatLevel,
                runState.IsGameOver != 0,
                autoAttackEnabled);
        }
    }
}
