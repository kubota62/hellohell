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
        ConfigureText(unitText);
        ConfigureText(bulletText);
    }

    public void SetCount(
        int unitNum,
        int bulletNum,
        int eliteNum,
        int level,
        int experience,
        int experienceToNextLevel,
        int score,
        int health,
        int maxHealth,
        float elapsedSeconds,
        int threatLevel,
        bool isGameOver,
        bool autoAttackEnabled)
    {
        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var attackMode = autoAttackEnabled ? "AUTO" : "MANUAL";

        unitText.text =
            $"LV {level}   XP {experience}/{experienceToNextLevel}\n" +
            $"HP {Mathf.Max(0, health)}/{Mathf.Max(1, maxHealth)}   SCORE {score}\n" +
            $"TIME {minutes:00}:{seconds:00}   THREAT {threatLevel}" +
            (isGameOver ? "\nDEFEATED" : string.Empty);
        bulletText.text =
            $"ENEMIES {Mathf.Max(0, unitNum - 1)}   ELITES {Mathf.Max(0, eliteNum)}   " +
            $"SHOTS {bulletNum}   ATTACK {attackMode}";
    }

    static void ConfigureText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.lineSpacing = 0f;
    }
}
