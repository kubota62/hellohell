using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ECS で集計したユニット数、弾数、成長状況を画面左下の HUD に表示する。
/// 既存シーンでは TMP_Text が 2 つ近い位置に置かれているため、表示は 1 枠に集約する。
/// </summary>
public class HUDUnitCount : MonoBehaviour
{
    const float ChampionBarWidth = 720f;

    public TMP_Text unitText;
    public TMP_Text bulletText;

    GameObject championHealthRoot;
    RectTransform championHealthFill;
    TMP_Text championHealthLabel;
    GameObject runResultRoot;
    TMP_Text runResultLabel;
    GameObject upgradeChoiceRoot;
    TMP_Text upgradeChoiceLabel;

    void Awake()
    {
        ConfigureText(
            unitText,
            new Vector2(24f, 24f),
            new Vector2(1000f, 160f),
            28f);
        ConfigureText(
            bulletText,
            new Vector2(24f, 194f),
            new Vector2(1400f, 140f),
            22f);
        CreateChampionHealthBar();
        CreateRunResultOverlay();
        CreateUpgradeChoiceOverlay();
    }

    public void SetCount(
        int unitNum,
        int bulletNum,
        int eliteNum,
        int championNum,
        int championCurrentHealth,
        int championMaxHealth,
        int level,
        int experience,
        int experienceToNextLevel,
        int score,
        int health,
        int maxHealth,
        float elapsedSeconds,
        float durationSeconds,
        int threatLevel,
        bool isGameOver,
        bool isVictory,
        bool autoAttackEnabled,
        PlayerSkillStats skillStats,
        bool hasUpgradeChoice,
        PlayerUpgradeChoice upgradeChoice,
        string statusNotification)
    {
        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var remainingTotalSeconds = Mathf.Max(
            0,
            Mathf.CeilToInt(durationSeconds - elapsedSeconds));
        var remainingMinutes = remainingTotalSeconds / 60;
        var remainingSeconds = remainingTotalSeconds % 60;
        var attackMode = autoAttackEnabled ? "AUTO" : "MANUAL";
        UpdateChampionHealthBar(
            championNum,
            championCurrentHealth,
            championMaxHealth);
        UpdateRunResultOverlay(
            isGameOver,
            isVictory,
            minutes,
            seconds,
            level,
            score,
            threatLevel);
        UpdateUpgradeChoiceOverlay(
            hasUpgradeChoice,
            upgradeChoice,
            skillStats);

        unitText.text =
            $"LV {level}   XP {experience}/{experienceToNextLevel}\n" +
            $"HP {Mathf.Max(0, health)}/{Mathf.Max(1, maxHealth)}   SCORE {score}\n" +
            $"TIME {minutes:00}:{seconds:00}   LEFT {remainingMinutes:00}:{remainingSeconds:00}   " +
            $"THREAT {threatLevel}" +
            (isGameOver
                ? isVictory
                    ? "\nVICTORY!   PRESS R TO REPLAY"
                    : "\nDEFEATED   PRESS R TO RETRY"
                : string.Empty);
        bulletText.text =
            (string.IsNullOrEmpty(statusNotification)
                ? string.Empty
                : $"{statusNotification}\n") +
            $"ENEMIES {Mathf.Max(0, unitNum - 1)}   ELITES {Mathf.Max(0, eliteNum)}   " +
            $"CHAMPIONS {Mathf.Max(0, championNum)}   " +
            $"SHOTS {bulletNum}   ATTACK {attackMode}\n" +
            $"BUILD  DMG L{skillStats.DamageLevel} x{1f + skillStats.DamageMultiplierAdd:0.00}   " +
            $"HASTE L{skillStats.AttackSpeedLevel} +{skillStats.CooldownMultiplierReduction * 100f:0}%   " +
            $"AREA L{skillStats.AreaLevel} x{1f + skillStats.AreaMultiplierAdd:0.00}\n" +
            $"MOVE L{skillStats.MoveSpeedLevel} x{1f + skillStats.MoveSpeedMultiplierAdd:0.00}   " +
            $"REGEN L{skillStats.RegenerationLevel} {skillStats.HealthRegenerationPerSecond:0.0}/s\n" +
            $"FORT L{skillStats.MaxHealthLevel} +{skillStats.MaxHealthAdd:0} HP   " +
            $"MAGNET L{skillStats.PickupRangeLevel} +{skillStats.PickupRadiusAdd:0.0}m\n" +
            $"WEAPONS  BLADE L{skillStats.MeleeArcLevel}   " +
            $"BOLT L{skillStats.RapidBoltLevel}   " +
            $"LANCE L{skillStats.PiercingLanceLevel}   " +
            $"ORB L{skillStats.ExplosiveOrbLevel}\n" +
            $"CRIT L{skillStats.CriticalChanceLevel} {skillStats.CriticalChance * 100f:0}%   " +
            $"FEROCITY L{skillStats.CriticalDamageLevel} " +
            $"x{1.5f + skillStats.CriticalDamageMultiplierAdd:0.00}   " +
            $"ARMOR L{skillStats.ArmorLevel} -{skillStats.DamageReduction * 100f:0}%   " +
            $"MULTI L{skillStats.MultistrikeLevel} {skillStats.MultistrikeChance * 100f:0}%   " +
            $"EXEC L{skillStats.ExecutionerLevel} +{skillStats.ExecutionDamageMultiplierAdd * 100f:0}%\n" +
            $"SECOND WIND L{skillStats.SecondWindLevel}   " +
            $"READY {skillStats.SecondWindChargesRemaining}   " +
            $"WISDOM L{skillStats.WisdomLevel} " +
            $"x{1f + skillStats.ExperienceMultiplierAdd:0.00} XP   " +
            $"LONGSHOT L{skillStats.LongshotLevel} " +
            $"x{1f + skillStats.ProjectileLifetimeMultiplierAdd:0.00}   " +
            $"HUNTER L{skillStats.BossHunterLevel} " +
            $"+{skillStats.EliteDamageMultiplierAdd * 100f:0}%   " +
            $"PEN L{skillStats.PenetrationLevel} " +
            $"+{PlayerAutoSkillSystem.GetProjectilePierceAdd(skillStats)}";
    }

    void CreateUpgradeChoiceOverlay()
    {
        upgradeChoiceRoot = new GameObject(
            "Upgrade Choice Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        upgradeChoiceRoot.layer = gameObject.layer;
        upgradeChoiceRoot.transform.SetParent(transform, false);

        var rootRect = upgradeChoiceRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(920f, 500f);

        var background = upgradeChoiceRoot.GetComponent<Image>();
        background.color = new Color(0.025f, 0.02f, 0.035f, 0.95f);
        background.raycastTarget = false;

        var labelObject = new GameObject(
            "Choices",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(upgradeChoiceRoot.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(48f, 34f);
        labelRect.offsetMax = new Vector2(-48f, -34f);

        upgradeChoiceLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (unitText != null)
        {
            upgradeChoiceLabel.font = unitText.font;
        }
        upgradeChoiceLabel.alignment = TextAlignmentOptions.Center;
        upgradeChoiceLabel.fontStyle = FontStyles.Bold;
        upgradeChoiceLabel.fontSize = 27f;
        upgradeChoiceLabel.color = Color.white;
        upgradeChoiceLabel.textWrappingMode = TextWrappingModes.NoWrap;
        upgradeChoiceLabel.raycastTarget = false;

        upgradeChoiceRoot.SetActive(false);
    }

    void UpdateUpgradeChoiceOverlay(
        bool isActive,
        PlayerUpgradeChoice choice,
        PlayerSkillStats stats)
    {
        if (upgradeChoiceRoot == null)
        {
            return;
        }

        upgradeChoiceRoot.SetActive(isActive);
        if (!isActive)
        {
            return;
        }

        var queuedLevels = choice.PendingLevels > 1
            ? $"  <size=22>({choice.PendingLevels} PICKS)</size>"
            : string.Empty;
        var rerollHint = choice.RerollsRemaining > 0
            ? "<color=#7ED8FF>[R] REROLL</color>  <size=20>1 LEFT</size>"
            : "<color=#777788>[R] REROLL USED</color>";
        upgradeChoiceLabel.text =
            $"<color=#FFD75A><size=48>LEVEL UP!</size></color>{queuedLevels}\n" +
            "<size=22>CHOOSE AN UPGRADE — PRESS 1, 2 OR 3</size>\n\n" +
            rerollHint + "\n\n" +
            FormatUpgradeOption(1, choice.First, stats) + "\n\n" +
            FormatUpgradeOption(2, choice.Second, stats) + "\n\n" +
            FormatUpgradeOption(3, choice.Third, stats);
    }

    static string FormatUpgradeOption(
        int index,
        PlayerSkillMasterData skill,
        PlayerSkillStats stats)
    {
        var currentLevel = GetSkillLevel(skill.Kind, stats);
        var nextLevel = Mathf.Min(
            skill.MaxLevel > 0 ? skill.MaxLevel : int.MaxValue,
            currentLevel + Mathf.Max(1, skill.AddLevel));
        var effect = GetSkillEffectDescription(skill);
        return
            $"<color=#FFD75A>[{index}]</color>  " +
            $"<size=34>{GetUpgradeName(skill.Kind)}</size>  " +
            $"<color=#B7B7C8>L{currentLevel} → L{nextLevel}</color>\n" +
            $"<size=22>{effect}</size>";
    }

    static int GetSkillLevel(PlayerSkillKind kind, PlayerSkillStats stats)
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
            case PlayerSkillKind.BossHunter:
                return stats.BossHunterLevel;
            case PlayerSkillKind.Penetration:
                return stats.PenetrationLevel;
            case PlayerSkillKind.Damage:
            default:
                return stats.DamageLevel;
        }
    }

    static string GetUpgradeName(PlayerSkillKind kind)
    {
        switch (kind)
        {
            case PlayerSkillKind.AttackSpeed:
                return "QUICK HANDS";
            case PlayerSkillKind.MoveSpeed:
                return "SWIFT FEET";
            case PlayerSkillKind.Area:
                return "REACH";
            case PlayerSkillKind.Regeneration:
                return "VITALITY";
            case PlayerSkillKind.MaxHealth:
                return "FORTITUDE";
            case PlayerSkillKind.PickupRange:
                return "MAGNETISM";
            case PlayerSkillKind.MeleeArc:
                return "BLADE MASTERY";
            case PlayerSkillKind.RapidBolt:
                return "RAPID BOLT";
            case PlayerSkillKind.PiercingLance:
                return "PIERCING LANCE";
            case PlayerSkillKind.ExplosiveOrb:
                return "EXPLOSIVE ORB";
            case PlayerSkillKind.CriticalChance:
                return "KEEN EYE";
            case PlayerSkillKind.CriticalDamage:
                return "FEROCITY";
            case PlayerSkillKind.Armor:
                return "IRON SKIN";
            case PlayerSkillKind.Multistrike:
                return "MULTISTRIKE";
            case PlayerSkillKind.Executioner:
                return "EXECUTIONER";
            case PlayerSkillKind.SecondWind:
                return "SECOND WIND";
            case PlayerSkillKind.Wisdom:
                return "WISDOM";
            case PlayerSkillKind.Longshot:
                return "LONGSHOT";
            case PlayerSkillKind.BossHunter:
                return "BOSS HUNTER";
            case PlayerSkillKind.Penetration:
                return "PENETRATION";
            case PlayerSkillKind.Damage:
            default:
                return "MIGHT";
        }
    }

    static string GetSkillEffectDescription(PlayerSkillMasterData skill)
    {
        switch (skill.Kind)
        {
            case PlayerSkillKind.AttackSpeed:
                return $"Attack cooldown -{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.MoveSpeed:
                return $"Movement speed +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Area:
                return $"Attack area +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Regeneration:
                return $"Health regeneration +{skill.EffectPerLevel:0.0}/s";
            case PlayerSkillKind.MaxHealth:
                return $"Maximum health +{skill.EffectPerLevel:0}";
            case PlayerSkillKind.PickupRange:
                return $"Experience attraction +{skill.EffectPerLevel:0.0}m";
            case PlayerSkillKind.MeleeArc:
            case PlayerSkillKind.RapidBolt:
            case PlayerSkillKind.PiercingLance:
            case PlayerSkillKind.ExplosiveOrb:
                return $"Unlock or strengthen weapon; damage +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.CriticalChance:
                return $"Critical chance +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.CriticalDamage:
                return $"Critical damage +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Armor:
                return $"Damage taken -{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Multistrike:
                return $"Repeat attack chance +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Executioner:
                return $"Damage vs enemies below 30% HP +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.SecondWind:
                return $"Revive once at {skill.EffectPerLevel * 100f:0}% HP with 2s invulnerability";
            case PlayerSkillKind.Wisdom:
                return $"Experience gained +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Longshot:
                return $"Projectile travel range +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.BossHunter:
                return $"Damage vs Elite and Champion enemies +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Penetration:
                return $"Projectile pierce +{skill.EffectPerLevel:0}";
            case PlayerSkillKind.Damage:
            default:
                return $"Damage +{skill.EffectPerLevel * 100f:0}%";
        }
    }

    void CreateRunResultOverlay()
    {
        runResultRoot = new GameObject(
            "Run Result Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        runResultRoot.layer = gameObject.layer;
        runResultRoot.transform.SetParent(transform, false);

        var rootRect = runResultRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(680f, 280f);

        var background = runResultRoot.GetComponent<Image>();
        background.color = new Color(0.025f, 0.02f, 0.035f, 0.92f);
        background.raycastTarget = false;

        var labelObject = new GameObject(
            "Result",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(runResultRoot.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(28f, 24f);
        labelRect.offsetMax = new Vector2(-28f, -24f);

        runResultLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (unitText != null)
        {
            runResultLabel.font = unitText.font;
        }
        runResultLabel.alignment = TextAlignmentOptions.Center;
        runResultLabel.fontStyle = FontStyles.Bold;
        runResultLabel.fontSize = 30f;
        runResultLabel.color = Color.white;
        runResultLabel.textWrappingMode = TextWrappingModes.NoWrap;
        runResultLabel.raycastTarget = false;

        runResultRoot.SetActive(false);
    }

    void UpdateRunResultOverlay(
        bool isGameOver,
        bool isVictory,
        int minutes,
        int seconds,
        int level,
        int score,
        int threatLevel)
    {
        if (runResultRoot == null)
        {
            return;
        }

        runResultRoot.SetActive(isGameOver);
        if (!isGameOver)
        {
            return;
        }

        var title = isVictory ? "VICTORY" : "DEFEATED";
        var titleColor = isVictory ? "#FFD75A" : "#FF6767";
        runResultLabel.text =
            $"<color={titleColor}><size=52>{title}</size></color>\n" +
            $"SURVIVED  {minutes:00}:{seconds:00}    LEVEL  {level}\n" +
            $"SCORE  {score}    THREAT  {threatLevel}\n" +
            "<size=24>PRESS R TO PLAY AGAIN</size>";
    }

    void CreateChampionHealthBar()
    {
        championHealthRoot = new GameObject(
            "Champion Health Bar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        championHealthRoot.layer = gameObject.layer;
        championHealthRoot.transform.SetParent(transform, false);

        var rootRect = championHealthRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -24f);
        rootRect.sizeDelta = new Vector2(ChampionBarWidth, 32f);

        var background = championHealthRoot.GetComponent<Image>();
        background.color = new Color(0.05f, 0.01f, 0.08f, 0.88f);
        background.raycastTarget = false;

        var fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObject.layer = gameObject.layer;
        fillObject.transform.SetParent(championHealthRoot.transform, false);
        championHealthFill = fillObject.GetComponent<RectTransform>();
        championHealthFill.anchorMin = new Vector2(0f, 0f);
        championHealthFill.anchorMax = new Vector2(0f, 1f);
        championHealthFill.pivot = new Vector2(0f, 0.5f);
        championHealthFill.anchoredPosition = new Vector2(3f, 0f);

        var fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.72f, 0.08f, 1f, 0.95f);
        fill.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(championHealthRoot.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        championHealthLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (unitText != null)
        {
            championHealthLabel.font = unitText.font;
        }
        championHealthLabel.fontSize = 18f;
        championHealthLabel.fontStyle = FontStyles.Bold;
        championHealthLabel.alignment = TextAlignmentOptions.Center;
        championHealthLabel.color = Color.white;
        championHealthLabel.raycastTarget = false;

        championHealthRoot.SetActive(false);
    }

    void UpdateChampionHealthBar(
        int championCount,
        int currentHealth,
        int maximumHealth)
    {
        if (championHealthRoot == null)
        {
            return;
        }

        var isVisible = championCount > 0 && maximumHealth > 0;
        championHealthRoot.SetActive(isVisible);
        if (!isVisible)
        {
            return;
        }

        var healthRatio = Mathf.Clamp01(
            (float)Mathf.Max(0, currentHealth) /
            Mathf.Max(1, maximumHealth));
        championHealthFill.sizeDelta = new Vector2(
            (ChampionBarWidth - 6f) * healthRatio,
            -6f);
        championHealthLabel.text = championCount > 1
            ? $"CHAMPIONS x{championCount}   {Mathf.Max(0, currentHealth)}/{maximumHealth}"
            : $"CHAMPION   {Mathf.Max(0, currentHealth)}/{maximumHealth}";
    }

    static void ConfigureText(
        TMP_Text text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
    {
        if (text == null)
        {
            return;
        }

        var rectTransform = text.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.zero;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        text.alignment = TextAlignmentOptions.BottomLeft;
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.lineSpacing = 0f;
        text.raycastTarget = false;
    }
}
