using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// EnemyRewardEventをPlayerProgressへ加算するシステム。
/// レベルアップ時は必要経験値を段階的に増やし、後続のスキル選択処理から参照できる状態にする。
/// </summary>
[BurstCompile]
public partial struct PlayerProgressSystem : ISystem
{
    const int InitialExperienceToNextLevel = 5;

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
                value.ExperienceToNextLevel = CalculateExperienceToNextLevel(value.Level);
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
        return new PlayerProgress
        {
            Level = 1,
            Experience = 0,
            ExperienceToNextLevel = InitialExperienceToNextLevel,
            Score = 0,
        };
    }

    static int CalculateExperienceToNextLevel(int level)
    {
        return InitialExperienceToNextLevel + math.max(0, level - 1) * 3;
    }
}
