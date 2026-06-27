using Unity.Entities;
using UnityEngine.Serialization;
using UnityEngine;

/// <summary>
/// ゲーム全体で共有するプレハブとスポーン設定を ECS の Config に焼き込む。
/// 旧フィールド名からの移行で既存シーンの参照が消えないように FormerlySerializedAs を付ける。
/// </summary>
public class ConfigAuthoring : MonoBehaviour
{
    [FormerlySerializedAs("TankPrefab")]
    public GameObject ActorPrefab;
    [FormerlySerializedAs("BulletPrefab")]
    public GameObject ProjectilePrefab;
    public GameObject DamageDigitPrefab;

    [Header("Enemy Spawn")]
    public float SpawnTime = 0.5f;
    public float MinSpawnDistance = 15f;
    public float MaxSpawnDistance = 25f;

    [Header("Spatial Hash")]
    public float SpatialHashCellSize = 4f;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                ActorPrefab = GetEntity(authoring.ActorPrefab, TransformUsageFlags.Dynamic),
                ProjectilePrefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                DamageDigitPrefab = authoring.DamageDigitPrefab != null
                    ? GetEntity(authoring.DamageDigitPrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null,
                SpawnTime = authoring.SpawnTime,
                MinSpawnDistance = authoring.MinSpawnDistance,
                MaxSpawnDistance = authoring.MaxSpawnDistance,
            });

            AddComponent(entity, new SpatialHashSettings
            {
                CellSize = authoring.SpatialHashCellSize,
            });
        }
    }
}
