using Unity.Entities;

public struct Config : IComponentData
{
    public Entity ActorPrefab;
    public Entity ProjectilePrefab;
    public Entity DamageDigitPrefab;
    public float SpawnTime;
    public float MinSpawnDistance;
    public float MaxSpawnDistance;
}
