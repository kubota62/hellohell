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
        var hasSkillMasters = SystemAPI.TryGetSingletonBuffer<PlayerSkillMasterElement>(
            out var skillMasters,
            true);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (levelUpEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerLevelUpEvent>>()
                     .WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PlayerSkillStats>(levelUpEvent.ValueRO.Player))
            {
                var stats = SystemAPI.GetComponent<PlayerSkillStats>(levelUpEvent.ValueRO.Player);
                ApplyAutoSkills(
                    ref stats,
                    levelUpEvent.ValueRO.NewLevel,
                    levelUpEvent.ValueRO.LevelsGained,
                    hasSkillMasters,
                    skillMasters);
                ecb.SetComponent(levelUpEvent.ValueRO.Player, stats);
            }

            ecb.DestroyEntity(eventEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    static void ApplyAutoSkills(
        ref PlayerSkillStats stats,
        int newLevel,
        int levelsGained,
        bool hasSkillMasters,
        DynamicBuffer<PlayerSkillMasterElement> skillMasters)
    {
        for (var i = 0; i < levelsGained; i++)
        {
            var gainedLevel = newLevel - levelsGained + 1 + i;
            var skill = hasSkillMasters
                ? PlayerSkillMasterCatalog.PickAutoSkill(skillMasters, gainedLevel - 1)
                : PlayerSkillMasterCatalog.GetByFallbackOrder(gainedLevel - 1);
            ApplySkill(ref stats, skill);
        }
    }

    static void ApplySkill(ref PlayerSkillStats stats, PlayerSkillMasterData skill)
    {
        var addLevel = skill.AddLevel <= 0 ? 1 : skill.AddLevel;
        switch (skill.Kind)
        {
            case PlayerSkillKind.MoveSpeed:
                stats.MoveSpeedLevel = AddClampedLevel(stats.MoveSpeedLevel, addLevel, skill.MaxLevel);
                break;

            case PlayerSkillKind.AttackSpeed:
                stats.AttackSpeedLevel = AddClampedLevel(stats.AttackSpeedLevel, addLevel, skill.MaxLevel);
                break;

            case PlayerSkillKind.Damage:
            default:
                stats.DamageLevel = AddClampedLevel(stats.DamageLevel, addLevel, skill.MaxLevel);
                break;
        }
    }

    static int AddClampedLevel(int currentLevel, int addLevel, int maxLevel)
    {
        var nextLevel = currentLevel + addLevel;
        return maxLevel > 0 && nextLevel > maxLevel ? maxLevel : nextLevel;
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
