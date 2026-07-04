using Unity.Mathematics;

/// <summary>
/// スポーンマスタへアクセスするための仮カタログ。
/// MasterCatalogAuthoringが未配置でも、Configと同等の既定値でゲームを動かす。
/// </summary>
public static class SpawnMasterCatalog
{
    public static SpawnMasterData Get(SpawnMasterId id)
    {
        switch (id)
        {
            case SpawnMasterId.Default:
            default:
                return new SpawnMasterData
                {
                    Id = SpawnMasterId.Default,
                    SpawnInterval = 0.5f,
                    InitialEnemySpawnDelay = 1f,
                    MinSpawnDistance = 15f,
                    MaxSpawnDistance = 25f,
                };
        }
    }

    public static SpawnMasterData Get(Config config)
    {
        var minDistance = math.max(0f, config.MinSpawnDistance);
        return new SpawnMasterData
        {
            Id = SpawnMasterId.Default,
            SpawnInterval = math.max(0.01f, config.SpawnTime),
            InitialEnemySpawnDelay = 1f,
            MinSpawnDistance = minDistance,
            MaxSpawnDistance = math.max(minDistance, config.MaxSpawnDistance),
        };
    }

    public static SpawnMasterData Get(
        Unity.Entities.DynamicBuffer<SpawnMasterElement> masters,
        SpawnMasterId id)
    {
        for (var i = 0; i < masters.Length; i++)
        {
            if (masters[i].Id == id)
            {
                return masters[i].ToRuntimeMaster();
            }
        }

        return Get(id);
    }
}
