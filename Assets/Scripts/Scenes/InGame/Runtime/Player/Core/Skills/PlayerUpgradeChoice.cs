using Unity.Entities;
using Unity.Mathematics;

public enum PlayerUpgradeQuality : byte
{
    Normal,
    Fine,
    Rare,
}

public static class PlayerUpgradeQualityUtility
{
    public static PlayerUpgradeQuality Resolve(
        int seed,
        int playerLevel,
        PlayerUpgradeSource source)
    {
        return Resolve(seed, playerLevel, source, 0);
    }

    public static PlayerUpgradeQuality Resolve(
        int seed,
        int playerLevel,
        PlayerUpgradeSource source,
        int fortuneLevel)
    {
        if (source == PlayerUpgradeSource.ChampionTreasure)
        {
            return PlayerUpgradeQuality.Rare;
        }

        var safeLevel = math.max(1, playerLevel);
        var safeFortuneLevel = math.clamp(fortuneLevel, 0, 3);
        var rareChance =
            math.min(18, 6 + safeLevel / 6) +
            safeFortuneLevel * 2;
        var fineChance =
            math.min(38, 24 + safeLevel / 8) +
            safeFortuneLevel * 3;
        var roll = (int)(math.hash(new int3(
            seed,
            safeLevel,
            (int)source + 97)) % 100u);
        if (roll < rareChance)
        {
            return PlayerUpgradeQuality.Rare;
        }

        return roll < rareChance + fineChance
            ? PlayerUpgradeQuality.Fine
            : PlayerUpgradeQuality.Normal;
    }

    public static float ResolveEffectMultiplier(PlayerUpgradeQuality quality)
    {
        return quality == PlayerUpgradeQuality.Rare
            ? 1.75f
            : quality == PlayerUpgradeQuality.Fine ? 1.35f : 1f;
    }

    public static int ResolveFortuneQualityBonus(int fortuneLevel)
    {
        return math.clamp(fortuneLevel, 0, 3) * 5;
    }

    public static PlayerSkillMasterData Apply(
        PlayerSkillMasterData skill,
        PlayerUpgradeQuality quality)
    {
        skill.EffectPerLevel = math.max(0f, skill.EffectPerLevel) *
            ResolveEffectMultiplier(quality);
        return skill;
    }
}

/// <summary>
/// レベルアップ中にプレイヤーへ提示する3つの強化候補。
/// PendingLevels が残っている場合は、選択後に次の候補を続けて提示する。
/// </summary>
public struct PlayerUpgradeChoice : IComponentData
{
    public Entity Player;
    public PlayerSkillMasterData First;
    public PlayerSkillMasterData Second;
    public PlayerSkillMasterData Third;
    public PlayerUpgradeQuality FirstQuality;
    public PlayerUpgradeQuality SecondQuality;
    public PlayerUpgradeQuality ThirdQuality;
    public int NewLevel;
    public int PendingLevels;
    public int RerollsRemaining;
    public int RerollGeneration;
    public int BanishesRemaining;
    public PlayerUpgradeSource Source;
}

/// <summary>
/// Managed入力からECSへ渡すレベルアップ選択結果。ChoiceIndex は0～2。
/// </summary>
public struct PlayerUpgradeSelection : IComponentData
{
    public int ChoiceIndex;
}

public struct PlayerUpgradeReroll : IComponentData
{
}

public struct PlayerUpgradeBanish : IComponentData
{
    public int ChoiceIndex;
}
