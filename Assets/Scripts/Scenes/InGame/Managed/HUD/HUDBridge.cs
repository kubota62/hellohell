using Unity.Entities;
using UnityEngine;

public class HUDBridge : MonoBehaviour
{
    public static HUDBridge Instance;

    public HUDUnitCount unitCount;

    int tankCount;
    int bulletCount;
    
    void Awake()
    {
        Instance = this;
    }

    public void SetTankCount(int count)
    {
        tankCount = count;
        unitCount.SetCount(tankCount, bulletCount);
    }

    public void SetBulletCount(int count)
    {
        bulletCount = count;
        unitCount.SetCount(tankCount, bulletCount);
    }
}