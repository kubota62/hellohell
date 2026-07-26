using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Player/Variants/Skills/Auto の自動スキル付与システム。
/// PlayerLevelUpEventをスキル強化へ変換する。
/// 現時点では選択UIがないため、マスタ候補から自動でスキル強化を選ぶ。
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
                    skillMasters,
                    levelUpEvent.ValueRO.Player,
                    ecb);
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
        DynamicBuffer<PlayerSkillMasterElement> skillMasters,
        Entity playerEntity,
        EntityCommandBuffer ecb)
    {
        for (var i = 0; i < levelsGained; i++)
        {
            var gainedLevel = newLevel - levelsGained + 1 + i;
            var skill = hasSkillMasters
                ? PlayerSkillMasterCatalog.PickAutoSkill(skillMasters, gainedLevel - 1, stats)
                : PlayerSkillMasterCatalog.GetByFallbackOrder(gainedLevel - 1, stats);
            var appliedLevel = ApplySkill(ref stats, skill);
            if (appliedLevel > 0)
            {
                CreateSkillAppliedEvent(playerEntity, skill, appliedLevel, GetCurrentSkillLevel(stats, skill.Kind), ecb);
            }
        }
    }

    static int ApplySkill(ref PlayerSkillStats stats, PlayerSkillMasterData skill)
    {
        var addLevel = skill.AddLevel <= 0 ? 1 : skill.AddLevel;
        switch (skill.Kind)
        {
            case PlayerSkillKind.MoveSpeed:
                return AddSkillLevel(
                    ref stats.MoveSpeedLevel,
                    ref stats.MoveSpeedMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.AttackSpeed:
                return AddSkillLevel(
                    ref stats.AttackSpeedLevel,
                    ref stats.CooldownMultiplierReduction,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Area:
                return AddSkillLevel(
                    ref stats.AreaLevel,
                    ref stats.AreaMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Regeneration:
                return AddSkillLevel(
                    ref stats.RegenerationLevel,
                    ref stats.HealthRegenerationPerSecond,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Damage:
            default:
                return AddSkillLevel(
                    ref stats.DamageLevel,
                    ref stats.DamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);
        }
    }

    static int AddSkillLevel(
        ref int currentLevel,
        ref float currentEffect,
        int addLevel,
        int maxLevel,
        float effectPerLevel)
    {
        var nextLevel = currentLevel + addLevel;
        if (maxLevel > 0 && nextLevel > maxLevel)
        {
            nextLevel = maxLevel;
        }

        var gainedLevel = nextLevel - currentLevel;
        currentLevel = nextLevel;
        currentEffect += gainedLevel * math.max(0f, effectPerLevel);
        return gainedLevel;
    }

    static void CreateSkillAppliedEvent(
        Entity playerEntity,
        PlayerSkillMasterData skill,
        int addedLevel,
        int newSkillLevel,
        EntityCommandBuffer ecb)
    {
        var eventEntity = ecb.CreateEntity();
        ecb.AddComponent(eventEntity, new PlayerSkillAppliedEvent
        {
            Player = playerEntity,
            SkillId = skill.Id,
            Kind = skill.Kind,
            AddedLevel = addedLevel,
            NewSkillLevel = newSkillLevel,
        });
    }

    static int GetCurrentSkillLevel(PlayerSkillStats stats, PlayerSkillKind kind)
    {
        switch (kind)
        {
            case PlayerSkillKind.MoveSpeed:
                return stats.MoveSpeedLevel;

            case PlayerSkillKind.AttackSpeed:
                return stats.AttackSpeedLevel;

            case PlayerSkillKind.Area:
                return stats.AreaLevel;

            case PlayerSkillKind.Regeneration:
                return stats.RegenerationLevel;

            case PlayerSkillKind.Damage:
            default:
                return stats.DamageLevel;
        }
    }

    public static float GetDamageMultiplier(in PlayerSkillStats stats)
    {
        return 1f + stats.DamageMultiplierAdd;
    }

    public static float GetCooldownMultiplier(in PlayerSkillStats stats)
    {
        var multiplier = 1f - stats.CooldownMultiplierReduction;
        return multiplier < 0.25f ? 0.25f : multiplier;
    }

    public static float GetMoveSpeedMultiplier(in PlayerSkillStats stats)
    {
        return 1f + stats.MoveSpeedMultiplierAdd;
    }

    public static float GetAreaMultiplier(in PlayerSkillStats stats)
    {
        return 1f + stats.AreaMultiplierAdd;
    }
}

/// <summary>
/// レベルアップで得た生命再生をプレイヤーのHealthへ反映する。
/// 小数回復量を蓄積し、1以上になった時点で整数HPへ変換する。
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(PlayerAutoSkillSystem))]
public partial struct PlayerHealthRegenerationSystem : ISystem
{
    private float accumulatedHealing;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (health, stats) in
                 SystemAPI.Query<RefRW<Health>, RefRO<PlayerSkillStats>>()
                     .WithAll<Player>())
        {
            if (health.ValueRO.Current >= health.ValueRO.Max)
            {
                accumulatedHealing = 0f;
                continue;
            }

            var regeneration = math.max(
                0f,
                stats.ValueRO.HealthRegenerationPerSecond);
            if (regeneration <= 0f)
            {
                continue;
            }

            accumulatedHealing += regeneration * SystemAPI.Time.DeltaTime;
            var healing = (int)math.floor(accumulatedHealing);
            if (healing <= 0)
            {
                continue;
            }

            var value = health.ValueRO;
            value.Current = math.min(value.Max, value.Current + healing);
            health.ValueRW = value;
            accumulatedHealing -= healing;
        }
    }
}
