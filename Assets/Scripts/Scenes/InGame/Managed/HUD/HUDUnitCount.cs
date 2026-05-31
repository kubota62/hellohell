using TMPro;
using Unity.Entities;
using UnityEngine;

public class HUDUnitCount : MonoBehaviour
{
    public TMP_Text unitText;
    public TMP_Text bulletText;
    
    public void SetCount(int unitNum, int bulletNum)
    {
        unitText.text = $"Tank Count: {unitNum}";
        bulletText.text = $"Bullet Count: {bulletNum}";
    }
}