using TMPro;
using UnityEngine;

/// <summary>
/// ECS で集計したユニット数、弾数、成長状況を画面左下の HUD に表示する。
/// 既存シーンでは TMP_Text が 2 つ近い位置に置かれているため、表示は 1 枠に集約する。
/// </summary>
public class HUDUnitCount : MonoBehaviour
{
    public TMP_Text unitText;
    public TMP_Text bulletText;

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
    }

    public void SetCount(
        int unitNum,
        int bulletNum,
        int eliteNum,
        int championNum,
        int level,
        int experience,
        int experienceToNextLevel,
        int score,
        int health,
        int maxHealth,
        float elapsedSeconds,
        int threatLevel,
        bool isGameOver,
        bool autoAttackEnabled,
        PlayerSkillStats skillStats,
        string statusNotification)
    {
        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var attackMode = autoAttackEnabled ? "AUTO" : "MANUAL";

        unitText.text =
            $"LV {level}   XP {experience}/{experienceToNextLevel}\n" +
            $"HP {Mathf.Max(0, health)}/{Mathf.Max(1, maxHealth)}   SCORE {score}\n" +
            $"TIME {minutes:00}:{seconds:00}   THREAT {threatLevel}" +
            (isGameOver ? "\nDEFEATED   PRESS R TO RETRY" : string.Empty);
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
