using Unity.Burst;
using Unity.Entities;

/// <summary>
/// PlayerLevelUpEventを仮スキルへ変換するシステム。
/// 現時点では選択UIがないため、ダメージ、攻撃速度、移動速度を順番に自動強化する。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(PlayerProgressSystem))]
public partial struct PlayerAutoSkillSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (levelUpEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerLevelUpEvent>>()
                     .WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PlayerSkillStats>(levelUpEvent.ValueRO.Player))
            {
                var stats = SystemAPI.GetComponent<PlayerSkillStats>(levelUpEvent.ValueRO.Player);
                ApplyAutoSkills(ref stats, levelUpEvent.ValueRO.NewLevel, levelUpEvent.ValueRO.LevelsGained);
                ecb.SetComponent(levelUpEvent.ValueRO.Player, stats);
            }

            ecb.DestroyEntity(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    static void ApplyAutoSkills(ref PlayerSkillStats stats, int newLevel, int levelsGained)
    {
        for (var i = 0; i < levelsGained; i++)
        {
            var gainedLevel = newLevel - levelsGained + 1 + i;
            switch (gainedLevel % 3)
            {
                case 0:
                    stats.MoveSpeedLevel++;
                    break;

                case 1:
                    stats.DamageLevel++;
                    break;

                case 2:
                default:
                    stats.AttackSpeedLevel++;
                    break;
            }
        }
    }

    public static float GetDamageMultiplier(in PlayerSkillStats stats)
    {
        return 1f + stats.DamageLevel * 0.15f;
    }

    public static float GetCooldownMultiplier(in PlayerSkillStats stats)
    {
        var multiplier = 1f - stats.AttackSpeedLevel * 0.08f;
        return multiplier < 0.25f ? 0.25f : multiplier;
    }

    public static float GetMoveSpeedMultiplier(in PlayerSkillStats stats)
    {
        return 1f + stats.MoveSpeedLevel * 0.1f;
    }
}
