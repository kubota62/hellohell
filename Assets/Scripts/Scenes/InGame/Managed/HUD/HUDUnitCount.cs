using TMPro;
using Unity.Entities;
using UnityEngine;

public class HUDUnitCount : MonoBehaviour
{
    public TMP_Text unitText;
    public TMP_Text bulletText;
    
    public void SetCount(int unitNum, int bulletNum)
    {
        unitText.text = $"Actor数: {unitNum}";
        bulletText.text = $"Projectile数: {bulletNum}";
    }
}
