using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 敵スポーンの間隔や出現距離を管理するScriptableObject。
/// 未入力の値は既定カタログ値を使い、シーン側の調整漏れで極端な値にならないようにする。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Masters/Spawn Master")]
public class SpawnMasterAsset : ScriptableObject
{
    public SpawnMasterId Id = SpawnMasterId.Default;
    public float SpawnInterval = 0.5f;
    public float InitialEnemySpawnDelay = 1f;
    public float MinSpawnDistance = 15f;
    public float MaxSpawnDistance = 25f;

    public SpawnMasterData ToRuntimeMaster()
    {
        var fallback = SpawnMasterCatalog.Get(Id);
        var spawnInterval = SpawnInterval > 0f ? SpawnInterval : fallback.SpawnInterval;
        var minDistance = MinSpawnDistance > 0f ? MinSpawnDistance : fallback.MinSpawnDistance;
        var maxDistance = MaxSpawnDistance > 0f ? MaxSpawnDistance : fallback.MaxSpawnDistance;

        return new SpawnMasterData
        {
            Id = Id,
            SpawnInterval = spawnInterval,
            InitialEnemySpawnDelay = math.max(0f, InitialEnemySpawnDelay),
            MinSpawnDistance = minDistance,
            MaxSpawnDistance = math.max(minDistance, maxDistance),
        };
    }
}
