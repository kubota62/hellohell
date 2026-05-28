using Unity.Entities;
using UnityEngine;

public class DamageBridge : MonoBehaviour
{
    public static DamageBridge Instance;

    public DamagePopupSpawner popup;

    void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(int value, Vector3 pos)
    {
        popup.Spawn(value, pos);
    }
}