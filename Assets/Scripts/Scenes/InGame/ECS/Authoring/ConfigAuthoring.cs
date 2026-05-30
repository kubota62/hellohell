using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject TankPrefab;
    public GameObject BulletPrefab;

    [Header("Enemy Spawn")]
    public float SpawnTime = 0.5f;
    public float MinSpawnDistance = 15f;
    public float MaxSpawnDistance = 25f;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                TankPrefab = GetEntity(authoring.TankPrefab, TransformUsageFlags.Dynamic),
                BulletPrefab = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic),
                SpawnTime = authoring.SpawnTime,
                MinSpawnDistance = authoring.MinSpawnDistance,
                MaxSpawnDistance = authoring.MaxSpawnDistance,
            });
        }
    }
}

public struct Config : IComponentData
{
    public Entity TankPrefab;
    public Entity BulletPrefab;
    public float SpawnTime;
    public float MinSpawnDistance;
    public float MaxSpawnDistance;
}
