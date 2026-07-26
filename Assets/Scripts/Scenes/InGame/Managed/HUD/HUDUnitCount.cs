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
            $"REGEN L{skillStats.RegenerationLevel} {skillStats.HealthRegenerationPerSecond:0.0}/s";
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
