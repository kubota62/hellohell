using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// プレイヤースキルマスタへアクセスするための既定カタログ。
/// MasterCatalogAuthoringが未配置でも、仮スキル強化は従来通り動く。
/// </summary>
public static class PlayerSkillMasterCatalog
{
    public static PlayerSkillMasterData Get(PlayerSkillMasterId id)
    {
        switch (id)
        {
            case PlayerSkillMasterId.AttackSpeedBoost:
                return Create(
                    PlayerSkillMasterId.AttackSpeedBoost,
                    PlayerSkillKind.AttackSpeed,
                    maxLevel: 10,
                    effectPerLevel: 0.08f,
                    weight: 1);

            case PlayerSkillMasterId.MoveSpeedBoost:
                return Create(
                    PlayerSkillMasterId.MoveSpeedBoost,
                    PlayerSkillKind.MoveSpeed,
                    maxLevel: 10,
                    effectPerLevel: 0.1f,
                    weight: 1);

            case PlayerSkillMasterId.AreaBoost:
                return Create(
                    PlayerSkillMasterId.AreaBoost,
                    PlayerSkillKind.Area,
                    maxLevel: 10,
                    effectPerLevel: 0.08f,
                    weight: 1);

            case PlayerSkillMasterId.RegenerationBoost:
                return Create(
                    PlayerSkillMasterId.RegenerationBoost,
                    PlayerSkillKind.Regeneration,
                    maxLevel: 10,
                    effectPerLevel: 0.4f,
                    weight: 1);

            case PlayerSkillMasterId.MaxHealthBoost:
                return Create(
                    PlayerSkillMasterId.MaxHealthBoost,
                    PlayerSkillKind.MaxHealth,
                    maxLevel: 10,
                    effectPerLevel: 15f,
                    weight: 1);

            case PlayerSkillMasterId.PickupRangeBoost:
                return Create(
                    PlayerSkillMasterId.PickupRangeBoost,
                    PlayerSkillKind.PickupRange,
                    maxLevel: 10,
                    effectPerLevel: 0.6f,
                    weight: 1);

            case PlayerSkillMasterId.MeleeArcMastery:
                return Create(
                    PlayerSkillMasterId.MeleeArcMastery,
                    PlayerSkillKind.MeleeArc,
                    maxLevel: 8,
                    effectPerLevel: 0.18f,
                    weight: 1);

            case PlayerSkillMasterId.RapidBoltMastery:
                return Create(
                    PlayerSkillMasterId.RapidBoltMastery,
                    PlayerSkillKind.RapidBolt,
                    maxLevel: 8,
                    effectPerLevel: 0.18f,
                    weight: 1);

            case PlayerSkillMasterId.PiercingLanceMastery:
                return Create(
                    PlayerSkillMasterId.PiercingLanceMastery,
                    PlayerSkillKind.PiercingLance,
                    maxLevel: 8,
                    effectPerLevel: 0.18f,
                    weight: 1);

            case PlayerSkillMasterId.ExplosiveOrbMastery:
                return Create(
                    PlayerSkillMasterId.ExplosiveOrbMastery,
                    PlayerSkillKind.ExplosiveOrb,
                    maxLevel: 8,
                    effectPerLevel: 0.18f,
                    weight: 1);

            case PlayerSkillMasterId.CriticalChanceBoost:
                return Create(
                    PlayerSkillMasterId.CriticalChanceBoost,
                    PlayerSkillKind.CriticalChance,
                    maxLevel: 8,
                    effectPerLevel: 0.04f,
                    weight: 1);

            case PlayerSkillMasterId.CriticalDamageBoost:
                return Create(
                    PlayerSkillMasterId.CriticalDamageBoost,
                    PlayerSkillKind.CriticalDamage,
                    maxLevel: 8,
                    effectPerLevel: 0.2f,
                    weight: 1);

            case PlayerSkillMasterId.ArmorBoost:
                return Create(
                    PlayerSkillMasterId.ArmorBoost,
                    PlayerSkillKind.Armor,
                    maxLevel: 10,
                    effectPerLevel: 0.05f,
                    weight: 1);

            case PlayerSkillMasterId.MultistrikeBoost:
                return Create(
                    PlayerSkillMasterId.MultistrikeBoost,
                    PlayerSkillKind.Multistrike,
                    maxLevel: 5,
                    effectPerLevel: 0.1f,
                    weight: 1);

            case PlayerSkillMasterId.ExecutionerBoost:
                return Create(
                    PlayerSkillMasterId.ExecutionerBoost,
                    PlayerSkillKind.Executioner,
                    maxLevel: 5,
                    effectPerLevel: 0.15f,
                    weight: 1);

            case PlayerSkillMasterId.SecondWindBoost:
                return Create(
                    PlayerSkillMasterId.SecondWindBoost,
                    PlayerSkillKind.SecondWind,
                    maxLevel: 1,
                    effectPerLevel: 0.4f,
                    weight: 1);

            case PlayerSkillMasterId.WisdomBoost:
                return Create(
                    PlayerSkillMasterId.WisdomBoost,
                    PlayerSkillKind.Wisdom,
                    maxLevel: 10,
                    effectPerLevel: 0.08f,
                    weight: 1);

            case PlayerSkillMasterId.LongshotBoost:
                return Create(
                    PlayerSkillMasterId.LongshotBoost,
                    PlayerSkillKind.Longshot,
                    maxLevel: 10,
                    effectPerLevel: 0.1f,
                    weight: 1);

            case PlayerSkillMasterId.DamageBoost:
            default:
                return Create(
                    PlayerSkillMasterId.DamageBoost,
                    PlayerSkillKind.Damage,
                    maxLevel: 20,
                    effectPerLevel: 0.15f,
                    weight: 1);
        }
    }

    public static PlayerSkillMasterData PickAutoSkill(
        DynamicBuffer<PlayerSkillMasterElement> masters,
        int pickIndex)
    {
        return PickAutoSkill(masters, pickIndex, default);
    }

    public static PlayerSkillMasterData PickAutoSkill(
        DynamicBuffer<PlayerSkillMasterElement> masters,
        int pickIndex,
        PlayerSkillStats stats)
    {
        if (masters.Length <= 0)
        {
            return GetByFallbackOrder(pickIndex, stats);
        }

        var totalWeight = 0;
        for (var i = 0; i < masters.Length; i++)
        {
            // 上限到達済みのスキルは候補から外し、レベルアップが空振りしないようにする。
            if (CanApply(masters[i].ToRuntimeMaster(), stats))
            {
                totalWeight += math.max(0, masters[i].Weight);
            }
        }

        if (totalWeight <= 0)
        {
            return GetByFallbackOrder(pickIndex, stats);
        }

        var targetWeight = PickWeightTarget(pickIndex, totalWeight);
        var accumulatedWeight = 0;
        for (var i = 0; i < masters.Length; i++)
        {
            var master = masters[i].ToRuntimeMaster();
            if (!CanApply(master, stats))
            {
                continue;
            }

            accumulatedWeight += math.max(0, masters[i].Weight);
            if (targetWeight < accumulatedWeight)
            {
                return master;
            }
        }

        return masters[masters.Length - 1].ToRuntimeMaster();
    }

    public static PlayerSkillMasterData GetByFallbackOrder(int pickIndex)
    {
        return GetByFallbackOrder(pickIndex, default);
    }

    public static PlayerSkillMasterData GetByFallbackOrder(int pickIndex, PlayerSkillStats stats)
    {
        const int fallbackCount = 19;
        var startIndex = (pickIndex < 0 ? 0 : pickIndex) % fallbackCount;
        for (var i = 0; i < fallbackCount; i++)
        {
            var id = GetFallbackId((startIndex + i) % fallbackCount);
            var master = Get(id);
            if (CanApply(master, stats))
            {
                return master;
            }
        }

        return Get(GetFallbackId(startIndex));
    }

    private static PlayerSkillMasterId GetFallbackId(int index)
    {
        switch (index)
        {
            case 1:
                return PlayerSkillMasterId.AttackSpeedBoost;

            case 2:
                return PlayerSkillMasterId.MoveSpeedBoost;

            case 3:
                return PlayerSkillMasterId.AreaBoost;

            case 4:
                return PlayerSkillMasterId.RegenerationBoost;

            case 5:
                return PlayerSkillMasterId.MaxHealthBoost;

            case 6:
                return PlayerSkillMasterId.PickupRangeBoost;

            case 7:
                return PlayerSkillMasterId.MeleeArcMastery;

            case 8:
                return PlayerSkillMasterId.RapidBoltMastery;

            case 9:
                return PlayerSkillMasterId.PiercingLanceMastery;

            case 10:
                return PlayerSkillMasterId.ExplosiveOrbMastery;

            case 11:
                return PlayerSkillMasterId.CriticalChanceBoost;

            case 12:
                return PlayerSkillMasterId.CriticalDamageBoost;

            case 13:
                return PlayerSkillMasterId.ArmorBoost;

            case 14:
                return PlayerSkillMasterId.MultistrikeBoost;

            case 15:
                return PlayerSkillMasterId.ExecutionerBoost;

            case 16:
                return PlayerSkillMasterId.SecondWindBoost;

            case 17:
                return PlayerSkillMasterId.WisdomBoost;

            case 18:
                return PlayerSkillMasterId.LongshotBoost;

            case 0:
            default:
                return PlayerSkillMasterId.DamageBoost;
        }
    }

    private static bool CanApply(PlayerSkillMasterData master, PlayerSkillStats stats)
    {
        if (master.MaxLevel <= 0)
        {
            return true;
        }

        return GetCurrentLevel(stats, master.Kind) < master.MaxLevel;
    }

    private static int GetCurrentLevel(PlayerSkillStats stats, PlayerSkillKind kind)
    {
        switch (kind)
        {
            case PlayerSkillKind.AttackSpeed:
                return stats.AttackSpeedLevel;

            case PlayerSkillKind.MoveSpeed:
                return stats.MoveSpeedLevel;

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

            case PlayerSkillKind.Damage:
            default:
                return stats.DamageLevel;
        }
    }

    private static int PickWeightTarget(int pickIndex, int totalWeight)
    {
        // Burstで扱いやすい決定的な疑似ランダム。将来選択UIが入るまではレベル番号を種にする。
        var safeIndex = (uint)math.max(0, pickIndex);
        var hash = math.hash(new uint2(safeIndex + 1u, 0x9E3779B9u));
        return (int)(hash % (uint)totalWeight);
    }

    private static PlayerSkillMasterData Create(
        PlayerSkillMasterId id,
        PlayerSkillKind kind,
        int maxLevel,
        float effectPerLevel,
        int weight)
    {
        return new PlayerSkillMasterData
        {
            Id = id,
            Kind = kind,
            AddLevel = 1,
            MaxLevel = maxLevel,
            EffectPerLevel = effectPerLevel,
            Weight = math.max(0, weight),
        };
    }
}
