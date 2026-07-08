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
        int level,
        int experience,
        int experienceToNextLevel,
        int score)
    {
        unitText.text = $"A:{unitNum} P:{bulletNum} Lv:{level} XP:{experience}/{experienceToNextLevel} S:{score}";
        bulletText.text = string.Empty;
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
