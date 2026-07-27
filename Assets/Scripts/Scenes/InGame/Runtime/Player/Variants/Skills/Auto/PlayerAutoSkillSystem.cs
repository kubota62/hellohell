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

        if (TryApplyBanish(
                ref state,
                hasSkillMasters,
                skillMasters,
                ecb))
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        if (TryApplyReroll(
                ref state,
                hasSkillMasters,
                skillMasters,
                ecb))
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        if (TryApplySelection(
                ref state,
                hasSkillMasters,
                skillMasters,
                ecb))
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        if (SystemAPI.HasSingleton<PlayerUpgradeChoice>())
        {
            ecb.Dispose();
            return;
        }

        foreach (var (levelUpEvent, eventEntity) in
                 SystemAPI.Query<RefRO<PlayerLevelUpEvent>>()
                     .WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PlayerSkillStats>(levelUpEvent.ValueRO.Player))
            {
                var stats = SystemAPI.GetComponent<PlayerSkillStats>(levelUpEvent.ValueRO.Player);
                var choice = CreateUpgradeChoice(
                    stats,
                    levelUpEvent.ValueRO.NewLevel,
                    levelUpEvent.ValueRO.LevelsGained,
                    hasSkillMasters,
                    skillMasters,
                    levelUpEvent.ValueRO.Player);
                var choiceEntity = ecb.CreateEntity();
                ecb.AddComponent(choiceEntity, choice);
                SetUpgradePause(true);
            }

            ecb.DestroyEntity(eventEntity);
            break;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    bool TryApplyBanish(
        ref SystemState state,
        bool hasSkillMasters,
        DynamicBuffer<PlayerSkillMasterElement> skillMasters,
        EntityCommandBuffer ecb)
    {
        foreach (var (banish, banishEntity) in
                 SystemAPI.Query<RefRO<PlayerUpgradeBanish>>()
                     .WithEntityAccess())
        {
            if (SystemAPI.TryGetSingletonEntity<PlayerUpgradeChoice>(
                    out var choiceEntity) &&
                SystemAPI.TryGetSingleton<PlayerUpgradeChoice>(
                    out var choice) &&
                choice.BanishesRemaining > 0 &&
                SystemAPI.HasComponent<PlayerSkillStats>(choice.Player))
            {
                var stats = SystemAPI.GetComponent<PlayerSkillStats>(
                    choice.Player);
                var skill = ResolveSelectedSkill(
                    choice,
                    banish.ValueRO.ChoiceIndex);
                if (TryAddBanishedSkill(
                        ref stats,
                        skill.Id))
                {
                    var nextChoice = CreateUpgradeChoice(
                        stats,
                        choice.NewLevel,
                        choice.PendingLevels,
                        hasSkillMasters,
                        skillMasters,
                        choice.Player,
                        choice.RerollGeneration + 1);
                    nextChoice.RerollsRemaining =
                        choice.RerollsRemaining;
                    nextChoice.BanishesRemaining =
                        math.max(
                            0,
                            choice.BanishesRemaining - 1);
                    ecb.SetComponent(choice.Player, stats);
                    ecb.SetComponent(choiceEntity, nextChoice);
                }
            }

            ecb.DestroyEntity(banishEntity);
            return true;
        }

        return false;
    }

    bool TryApplyReroll(
        ref SystemState state,
        bool hasSkillMasters,
        DynamicBuffer<PlayerSkillMasterElement> skillMasters,
        EntityCommandBuffer ecb)
    {
        foreach (var (_, rerollEntity) in
                 SystemAPI.Query<RefRO<PlayerUpgradeReroll>>()
                     .WithEntityAccess())
        {
            if (SystemAPI.TryGetSingletonEntity<PlayerUpgradeChoice>(
                    out var choiceEntity) &&
                SystemAPI.TryGetSingleton<PlayerUpgradeChoice>(
                    out var choice) &&
                choice.RerollsRemaining > 0 &&
                SystemAPI.HasComponent<PlayerSkillStats>(choice.Player))
            {
                var stats = SystemAPI.GetComponent<PlayerSkillStats>(
                    choice.Player);
                var rerolledChoice = CreateUpgradeChoice(
                    stats,
                    choice.NewLevel,
                    choice.PendingLevels,
                    hasSkillMasters,
                    skillMasters,
                    choice.Player,
                    choice.RerollGeneration + 1);
                rerolledChoice.RerollsRemaining =
                    choice.RerollsRemaining - 1;
                ecb.SetComponent(choiceEntity, rerolledChoice);
            }

            ecb.DestroyEntity(rerollEntity);
            return true;
        }

        return false;
    }

    bool TryApplySelection(
        ref SystemState state,
        bool hasSkillMasters,
        DynamicBuffer<PlayerSkillMasterElement> skillMasters,
        EntityCommandBuffer ecb)
    {
        if (!SystemAPI.TryGetSingletonEntity<PlayerUpgradeChoice>(out var choiceEntity) ||
            !SystemAPI.TryGetSingleton<PlayerUpgradeChoice>(out var choice))
        {
            return false;
        }

        foreach (var (selection, selectionEntity) in
                 SystemAPI.Query<RefRO<PlayerUpgradeSelection>>()
                     .WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PlayerSkillStats>(choice.Player))
            {
                var stats = SystemAPI.GetComponent<PlayerSkillStats>(choice.Player);
                var skill = ResolveSelectedSkill(choice, selection.ValueRO.ChoiceIndex);
                var appliedLevel = ApplySkill(ref stats, skill);
                if (appliedLevel > 0)
                {
                    ApplyImmediateHealthIncrease(
                        ref state,
                        choice.Player,
                        skill,
                        appliedLevel,
                        ecb);
                    CreateSkillAppliedEvent(
                        choice.Player,
                        skill,
                        appliedLevel,
                        GetCurrentSkillLevel(stats, skill.Kind),
                        ecb);
                }

                ecb.SetComponent(choice.Player, stats);
                choice.PendingLevels--;
                if (choice.PendingLevels > 0)
                {
                    ecb.SetComponent(
                        choiceEntity,
                        CreateUpgradeChoice(
                            stats,
                            choice.NewLevel,
                            choice.PendingLevels,
                            hasSkillMasters,
                            skillMasters,
                            choice.Player));
                }
                else
                {
                    ecb.DestroyEntity(choiceEntity);
                    SetUpgradePause(false);
                }
            }
            else
            {
                ecb.DestroyEntity(choiceEntity);
                SetUpgradePause(false);
            }

            ecb.DestroyEntity(selectionEntity);
            return true;
        }

        return false;
    }

    static PlayerUpgradeChoice CreateUpgradeChoice(
        PlayerSkillStats stats,
        int newLevel,
        int pendingLevels,
        bool hasSkillMasters,
        DynamicBuffer<PlayerSkillMasterElement> skillMasters,
        Entity playerEntity,
        int rerollGeneration = 0)
    {
        var generationOffset = math.max(0, rerollGeneration) * 101;
        var first = PickDistinctSkill(
            stats,
            newLevel + pendingLevels * 11 + generationOffset,
            default,
            default,
            hasSkillMasters,
            skillMasters);
        var second = PickDistinctSkill(
            stats,
            newLevel + pendingLevels * 23 + generationOffset,
            first.Id,
            default,
            hasSkillMasters,
            skillMasters);
        var third = PickDistinctSkill(
            stats,
            newLevel + pendingLevels * 37 + generationOffset,
            first.Id,
            second.Id,
            hasSkillMasters,
            skillMasters);

        return new PlayerUpgradeChoice
        {
            Player = playerEntity,
            First = first,
            Second = second,
            Third = third,
            NewLevel = newLevel,
            PendingLevels = math.max(1, pendingLevels),
            RerollsRemaining = GetUpgradeRerollCount(stats),
            RerollGeneration = math.max(0, rerollGeneration),
            BanishesRemaining = GetUpgradeBanishCount(stats),
        };
    }

    static PlayerSkillMasterData PickDistinctSkill(
        PlayerSkillStats stats,
        int seed,
        PlayerSkillMasterId excludedFirst,
        PlayerSkillMasterId excludedSecond,
        bool hasSkillMasters,
        DynamicBuffer<PlayerSkillMasterElement> skillMasters)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var skill = hasSkillMasters
                ? PlayerSkillMasterCatalog.PickAutoSkill(
                    skillMasters,
                    seed + attempt * 17,
                    stats)
                : PlayerSkillMasterCatalog.GetByFallbackOrder(
                    seed + attempt,
                    stats);
            if (skill.Id != excludedFirst && skill.Id != excludedSecond)
            {
                return skill;
            }
        }

        return hasSkillMasters
            ? PlayerSkillMasterCatalog.PickAutoSkill(skillMasters, seed, stats)
            : PlayerSkillMasterCatalog.GetByFallbackOrder(seed, stats);
    }

    static PlayerSkillMasterData ResolveSelectedSkill(
        PlayerUpgradeChoice choice,
        int choiceIndex)
    {
        switch (math.clamp(choiceIndex, 0, 2))
        {
            case 1:
                return choice.Second;

            case 2:
                return choice.Third;

            default:
                return choice.First;
        }
    }

    void SetUpgradePause(bool isPaused)
    {
        if (!SystemAPI.TryGetSingleton<RunState>(out var runState))
        {
            return;
        }

        runState.IsChoosingUpgrade = isPaused ? (byte)1 : (byte)0;
        SystemAPI.SetSingleton(runState);
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

            case PlayerSkillKind.MaxHealth:
                return AddSkillLevel(
                    ref stats.MaxHealthLevel,
                    ref stats.MaxHealthAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.PickupRange:
                return AddSkillLevel(
                    ref stats.PickupRangeLevel,
                    ref stats.PickupRadiusAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.MeleeArc:
                return AddSkillLevel(
                    ref stats.MeleeArcLevel,
                    ref stats.MeleeArcDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.RapidBolt:
                return AddSkillLevel(
                    ref stats.RapidBoltLevel,
                    ref stats.RapidBoltDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.PiercingLance:
                return AddSkillLevel(
                    ref stats.PiercingLanceLevel,
                    ref stats.PiercingLanceDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.ExplosiveOrb:
                return AddSkillLevel(
                    ref stats.ExplosiveOrbLevel,
                    ref stats.ExplosiveOrbDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.CriticalChance:
                return AddSkillLevel(
                    ref stats.CriticalChanceLevel,
                    ref stats.CriticalChance,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.CriticalDamage:
                return AddSkillLevel(
                    ref stats.CriticalDamageLevel,
                    ref stats.CriticalDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Armor:
                return AddSkillLevel(
                    ref stats.ArmorLevel,
                    ref stats.DamageReduction,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Multistrike:
                return AddSkillLevel(
                    ref stats.MultistrikeLevel,
                    ref stats.MultistrikeChance,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Executioner:
                return AddSkillLevel(
                    ref stats.ExecutionerLevel,
                    ref stats.ExecutionDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.SecondWind:
            {
                var gainedLevel = AddSkillLevel(
                    ref stats.SecondWindLevel,
                    ref stats.RevivalHealthFraction,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);
                stats.SecondWindChargesRemaining += gainedLevel;
                return gainedLevel;
            }

            case PlayerSkillKind.Wisdom:
                return AddSkillLevel(
                    ref stats.WisdomLevel,
                    ref stats.ExperienceMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Longshot:
                return AddSkillLevel(
                    ref stats.LongshotLevel,
                    ref stats.ProjectileLifetimeMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.BossHunter:
                return AddSkillLevel(
                    ref stats.BossHunterLevel,
                    ref stats.EliteDamageMultiplierAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Penetration:
                return AddSkillLevel(
                    ref stats.PenetrationLevel,
                    ref stats.ProjectilePierceAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Fortune:
                return AddSkillLevel(
                    ref stats.FortuneLevel,
                    ref stats.UpgradeRerollsAdd,
                    addLevel,
                    skill.MaxLevel,
                    skill.EffectPerLevel);

            case PlayerSkillKind.Berserker:
                return AddSkillLevel(
                    ref stats.BerserkerLevel,
                    ref stats.LowHealthDamageMultiplierAdd,
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

    void ApplyImmediateHealthIncrease(
        ref SystemState state,
        Entity playerEntity,
        PlayerSkillMasterData skill,
        int appliedLevel,
        EntityCommandBuffer ecb)
    {
        if (skill.Kind != PlayerSkillKind.MaxHealth ||
            !SystemAPI.HasComponent<Health>(playerEntity))
        {
            return;
        }

        var health = SystemAPI.GetComponent<Health>(playerEntity);
        var healthIncrease = math.max(
            1,
            (int)math.round(
                appliedLevel * math.max(0f, skill.EffectPerLevel)));
        health.Max += healthIncrease;
        health.Current = math.min(
            health.Max,
            health.Current + healthIncrease);
        ecb.SetComponent(playerEntity, health);
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

            case PlayerSkillKind.MaxHealth:
                return stats.MaxHealthLevel;

            case PlayerSkillKind.PickupRange:
                return stats.PickupRangeLevel;

            case PlayerSkillKind.MeleeArc:
                return stats.MeleeArcLevel;

            case PlayerSkillKind.RapidBolt:
                return stats.RapidBoltLevel;

            case PlayerSkillKind.PiercingLance:
                return stats.PiercingLanceLevel;

            case PlayerSkillKind.ExplosiveOrb:
                return stats.ExplosiveOrbLevel;

            case PlayerSkillKind.CriticalChance:
                return stats.CriticalChanceLevel;

            case PlayerSkillKind.CriticalDamage:
                return stats.CriticalDamageLevel;

            case PlayerSkillKind.Armor:
                return stats.ArmorLevel;

            case PlayerSkillKind.Multistrike:
                return stats.MultistrikeLevel;

            case PlayerSkillKind.Executioner:
                return stats.ExecutionerLevel;

            case PlayerSkillKind.SecondWind:
                return stats.SecondWindLevel;

            case PlayerSkillKind.Wisdom:
                return stats.WisdomLevel;

            case PlayerSkillKind.Longshot:
                return stats.LongshotLevel;

            case PlayerSkillKind.BossHunter:
                return stats.BossHunterLevel;

            case PlayerSkillKind.Penetration:
                return stats.PenetrationLevel;

            case PlayerSkillKind.Fortune:
                return stats.FortuneLevel;

            case PlayerSkillKind.Berserker:
                return stats.BerserkerLevel;

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

    public static int GetWeaponLevel(
        in PlayerSkillStats stats,
        AttackMasterId attackMasterId)
    {
        switch (attackMasterId)
        {
            case AttackMasterId.BasicMeleeArc:
                return stats.MeleeArcLevel;

            case AttackMasterId.RapidBolt:
                return stats.RapidBoltLevel;

            case AttackMasterId.PiercingLance:
                return stats.PiercingLanceLevel;

            case AttackMasterId.ExplosiveOrb:
                return stats.ExplosiveOrbLevel;

            default:
                return 1;
        }
    }

    public static float GetWeaponDamageMultiplier(
        in PlayerSkillStats stats,
        AttackMasterId attackMasterId)
    {
        switch (attackMasterId)
        {
            case AttackMasterId.BasicMeleeArc:
                return 1f + stats.MeleeArcDamageMultiplierAdd;

            case AttackMasterId.RapidBolt:
                return 1f + stats.RapidBoltDamageMultiplierAdd;

            case AttackMasterId.PiercingLance:
                return 1f + stats.PiercingLanceDamageMultiplierAdd;

            case AttackMasterId.ExplosiveOrb:
                return 1f + stats.ExplosiveOrbDamageMultiplierAdd;

            default:
                return 1f;
        }
    }

    public static float GetCriticalChance(in PlayerSkillStats stats)
    {
        return math.clamp(stats.CriticalChance, 0f, 0.75f);
    }

    public static float GetCriticalDamageMultiplier(in PlayerSkillStats stats)
    {
        return math.max(1.5f, 1.5f + stats.CriticalDamageMultiplierAdd);
    }

    public static float GetDamageReduction(in PlayerSkillStats stats)
    {
        return math.clamp(stats.DamageReduction, 0f, 0.65f);
    }

    public static float GetMultistrikeChance(in PlayerSkillStats stats)
    {
        return math.clamp(stats.MultistrikeChance, 0f, 1f);
    }

    public static float GetExecutionDamageBonus(in PlayerSkillStats stats)
    {
        return math.clamp(stats.ExecutionDamageMultiplierAdd, 0f, 1.5f);
    }

    public static float GetExperienceMultiplier(in PlayerSkillStats stats)
    {
        return 1f + math.clamp(
            stats.ExperienceMultiplierAdd,
            0f,
            2f);
    }

    public static float GetProjectileLifetimeMultiplier(
        in PlayerSkillStats stats)
    {
        return 1f + math.clamp(
            stats.ProjectileLifetimeMultiplierAdd,
            0f,
            2f);
    }

    public static float GetEliteDamageBonus(in PlayerSkillStats stats)
    {
        return math.clamp(
            stats.EliteDamageMultiplierAdd,
            0f,
            2f);
    }

    public static int GetProjectilePierceAdd(in PlayerSkillStats stats)
    {
        return math.clamp(
            (int)math.round(stats.ProjectilePierceAdd),
            0,
            8);
    }

    public static int GetUpgradeRerollCount(in PlayerSkillStats stats)
    {
        return 1 + math.clamp(
            (int)math.round(stats.UpgradeRerollsAdd),
            0,
            3);
    }

    public static int GetUpgradeBanishCount(in PlayerSkillStats stats)
    {
        return math.clamp(
            1 - stats.BanishedSkillCount,
            0,
            1);
    }

    public static bool TryAddBanishedSkill(
        ref PlayerSkillStats stats,
        PlayerSkillMasterId skillId)
    {
        if (skillId == default ||
            stats.IsSkillBanished(skillId))
        {
            return false;
        }

        if (stats.BanishedSkillFirst == default)
        {
            stats.BanishedSkillFirst = skillId;
        }
        else if (stats.BanishedSkillSecond == default)
        {
            stats.BanishedSkillSecond = skillId;
        }
        else if (stats.BanishedSkillThird == default)
        {
            stats.BanishedSkillThird = skillId;
        }
        else
        {
            return false;
        }

        stats.BanishedSkillCount = math.clamp(
            stats.BanishedSkillCount + 1,
            0,
            3);
        return true;
    }

    public static float GetLowHealthDamageBonus(in PlayerSkillStats stats)
    {
        return math.clamp(
            stats.LowHealthDamageMultiplierAdd,
            0f,
            1f);
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
        if (SystemAPI.TryGetSingleton<RunState>(out var runState) &&
            (runState.IsGameOver != 0 || runState.IsChoosingUpgrade != 0))
        {
            return;
        }

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
