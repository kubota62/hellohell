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
        var startIndex = (pickIndex < 0 ? 0 : pickIndex) % 5;
        for (var i = 0; i < 5; i++)
        {
            var id = GetFallbackId((startIndex + i) % 5);
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
