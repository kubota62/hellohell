using Unity.Entities;
using UnityEngine;

public class HUDBridge : MonoBehaviour
{
    public static HUDBridge Instance;

    public HUDUnitCount unitCount;

    int actorCount;
    int projectileCount;
    
    void Awake()
    {
        Instance = this;
    }

    public void SetActorCount(int count)
    {
        actorCount = count;
        unitCount.SetCount(actorCount, projectileCount);
    }

    public void SetProjectileCount(int count)
    {
        projectileCount = count;
        unitCount.SetCount(actorCount, projectileCount);
    }
}
