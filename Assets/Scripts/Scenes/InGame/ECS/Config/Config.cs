using Unity.Entities;

public struct Config : IComponentData
{
    public Entity TankPrefab;
    public Entity BulletPrefab;
    public Entity DamageDigitPrefab;
    public float SpawnTime;
    public float MinSpawnDistance;
    public float MaxSpawnDistance;
}
