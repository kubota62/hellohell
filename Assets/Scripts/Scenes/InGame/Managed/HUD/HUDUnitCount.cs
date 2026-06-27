using TMPro;
using UnityEngine;

/// <summary>
/// ECS で集計したユニット数と弾数を画面左下の HUD に表示する。
/// TMP の標準フォント警告を避けるため、表示文字列は ASCII に寄せる。
/// </summary>
public class HUDUnitCount : MonoBehaviour
{
    public TMP_Text unitText;
    public TMP_Text bulletText;

    public void SetCount(int unitNum, int bulletNum)
    {
        unitText.text = $"Actors: {unitNum}";
        bulletText.text = $"Projectiles: {bulletNum}";
    }
}
