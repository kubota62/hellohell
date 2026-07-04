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

        foreach (var progress in SystemAPI.Query<RefRW<PlayerProgress>>().WithAll<Player>())
        {
            var value = progress.ValueRO;
            value.Level = math.max(1, value.Level);
            value.ExperienceToNextLevel = math.max(1, value.ExperienceToNextLevel);
            value.Experience += totalExperience;
            value.Score += totalScore;

            while (value.Experience >= value.ExperienceToNextLevel)
            {
                value.Experience -= value.ExperienceToNextLevel;
                value.Level++;
                value.ExperienceToNextLevel = CalculateExperienceToNextLevel(value.Level);
            }

            progress.ValueRW = value;
        }
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
