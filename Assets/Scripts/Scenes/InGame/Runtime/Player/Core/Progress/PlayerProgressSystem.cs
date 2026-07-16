using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 経験値ドロップをプレイヤーへ吸引し、接触時に報酬イベントへ変換する。
/// </summary>
[UpdateBefore(typeof(PlayerProgressSystem))]
public partial struct ExperiencePickupSystem : ISystem
{
    private const float AttractionRadius = 2.75f;
    private const float CollectRadius = 1.2f;
    private const float MinimumMoveSpeed = 5f;
    private const float MaximumMoveSpeed = 14f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<ExperiencePickup>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var playerEntity = SystemAPI.GetSingletonEntity<Player>();
        if (!SystemAPI.HasComponent<LocalTransform>(playerEntity))
        {
            return;
        }

        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
        playerPosition.y = 0.35f;
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, pickup, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<ExperiencePickup>>()
                     .WithEntityAccess())
        {
            var toPlayer = playerPosition - transform.ValueRO.Position;
            var distanceSq = math.lengthsq(toPlayer);
            if (distanceSq > AttractionRadius * AttractionRadius)
            {
                continue;
            }

            if (distanceSq <= CollectRadius * CollectRadius)
            {
                var rewardEvent = ecb.CreateEntity();
                ecb.AddComponent(rewardEvent, new EnemyRewardEvent
                {
                    EnemyTypeId = 0,
                    Experience = pickup.ValueRO.Experience,
                    Score = pickup.ValueRO.Score,
                });
                ecb.DestroyEntity(entity);
                continue;
            }

            var distance = math.sqrt(distanceSq);
            var attraction = 1f - math.saturate(distance / AttractionRadius);
            var speed = math.lerp(MinimumMoveSpeed, MaximumMoveSpeed, attraction);
            transform.ValueRW.Position += math.normalizesafe(toPlayer) * speed * deltaTime;
            transform.ValueRW.Rotation = math.mul(
                quaternion.RotateY(deltaTime * 5f),
                transform.ValueRO.Rotation);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>
/// Player/Core/Progress の成長集計システム。
/// EnemyRewardEventをPlayerProgressへ加算する。
/// レベルアップ時は必要経験値を段階的に増やし、後続のスキル選択処理から参照できる状態にする。
/// </summary>
[BurstCompile]
public partial struct PlayerProgressSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerProgress>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var totalExperience = 0;
        var totalScore = 0;

        foreach (var reward in SystemAPI.Query<RefRO<EnemyRewardEvent>>())
        {
            totalExperience += reward.ValueRO.Experience;
            totalScore += reward.ValueRO.Score;
        }

        if (totalExperience <= 0 && totalScore <= 0)
        {
            return;
        }

        var progressMaster = ResolveProgressMaster(ref state);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach (var (progress, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerProgress>>()
                     .WithAll<Player>()
                     .WithEntityAccess())
        {
            var value = progress.ValueRO;
            value.Level = math.max(1, value.Level);
            value.ExperienceToNextLevel = math.max(1, value.ExperienceToNextLevel);
            value.Experience += totalExperience;
            value.Score += totalScore;
            var levelsGained = 0;

            while (value.Experience >= value.ExperienceToNextLevel)
            {
                value.Experience -= value.ExperienceToNextLevel;
                value.Level++;
                levelsGained++;
                value.ExperienceToNextLevel = CalculateExperienceToNextLevel(value.Level, progressMaster);
            }

            progress.ValueRW = value;

            if (levelsGained > 0)
            {
                var eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new PlayerLevelUpEvent
                {
                    Player = playerEntity,
                    NewLevel = value.Level,
                    LevelsGained = levelsGained,
                });
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static PlayerProgress CreateInitialProgress()
    {
        return CreateInitialProgress(PlayerProgressMasterCatalog.Get(PlayerProgressMasterId.Default));
    }

    public static PlayerProgress CreateInitialProgress(PlayerProgressMasterData master)
    {
        return new PlayerProgress
        {
            Level = math.max(1, master.InitialLevel),
            Experience = 0,
            ExperienceToNextLevel = math.max(1, master.InitialExperienceToNextLevel),
            Score = 0,
        };
    }

    private PlayerProgressMasterData ResolveProgressMaster(ref SystemState state)
    {
        if (SystemAPI.TryGetSingletonBuffer<PlayerProgressMasterElement>(out var masters, true))
        {
            return PlayerProgressMasterCatalog.Get(masters, PlayerProgressMasterId.Default);
        }

        return PlayerProgressMasterCatalog.Get(PlayerProgressMasterId.Default);
    }

    static int CalculateExperienceToNextLevel(int level, PlayerProgressMasterData master)
    {
        return math.max(1, master.InitialExperienceToNextLevel) +
            math.max(0, level - 1) * math.max(0, master.ExperienceToNextLevelAdd);
    }
}
