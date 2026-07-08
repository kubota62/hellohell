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
                    weight: 1);

            case PlayerSkillMasterId.MoveSpeedBoost:
                return Create(
                    PlayerSkillMasterId.MoveSpeedBoost,
                    PlayerSkillKind.MoveSpeed,
                    maxLevel: 10,
                    weight: 1);

            case PlayerSkillMasterId.DamageBoost:
            default:
                return Create(
                    PlayerSkillMasterId.DamageBoost,
                    PlayerSkillKind.Damage,
                    maxLevel: 20,
                    weight: 1);
        }
    }

    public static PlayerSkillMasterData PickAutoSkill(
        DynamicBuffer<PlayerSkillMasterElement> masters,
        int pickIndex)
    {
        if (masters.Length <= 0)
        {
            return GetByFallbackOrder(pickIndex);
        }

        var totalWeight = 0;
        for (var i = 0; i < masters.Length; i++)
        {
            totalWeight += math.max(0, masters[i].Weight);
        }

        if (totalWeight <= 0)
        {
            return masters[(pickIndex < 0 ? 0 : pickIndex) % masters.Length].ToRuntimeMaster();
        }

        var targetWeight = (pickIndex < 0 ? 0 : pickIndex) % totalWeight;
        var accumulatedWeight = 0;
        for (var i = 0; i < masters.Length; i++)
        {
            accumulatedWeight += math.max(0, masters[i].Weight);
            if (targetWeight < accumulatedWeight)
            {
                return masters[i].ToRuntimeMaster();
            }
        }

        return masters[masters.Length - 1].ToRuntimeMaster();
    }

    public static PlayerSkillMasterData GetByFallbackOrder(int pickIndex)
    {
        switch ((pickIndex < 0 ? 0 : pickIndex) % 3)
        {
            case 0:
                return Get(PlayerSkillMasterId.DamageBoost);

            case 1:
                return Get(PlayerSkillMasterId.AttackSpeedBoost);

            case 2:
            default:
                return Get(PlayerSkillMasterId.MoveSpeedBoost);
        }
    }

    private static PlayerSkillMasterData Create(
        PlayerSkillMasterId id,
        PlayerSkillKind kind,
        int maxLevel,
        int weight)
    {
        return new PlayerSkillMasterData
        {
            Id = id,
            Kind = kind,
            AddLevel = 1,
            MaxLevel = maxLevel,
            Weight = math.max(0, weight),
        };
    }
}
