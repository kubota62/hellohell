using Unity.Entities;

/// <summary>
/// Masters/Spawn のID定義。
/// スポーン設定マスタを参照するための軽量ID。
/// 将来ウェーブやステージごとにスポーン設定を切り替える入口にする。
/// </summary>
public enum SpawnMasterId
{
    Default = 1,
}

/// <summary>
/// 敵スポーンのタイミングと出現距離をまとめたランタイム用マスタ値。
/// Configはプレハブ参照を持ち、スポーン調整値はこのマスタへ寄せていく。
/// </summary>
public struct SpawnMasterData
{
    public SpawnMasterId Id;
    public float SpawnInterval;
    public float InitialEnemySpawnDelay;
    public float MinSpawnDistance;
    public float MaxSpawnDistance;
}

/// <summary>
/// BakerがScriptableObjectのスポーンマスタをECS側へ渡すためのバッファ要素。
/// </summary>
public struct SpawnMasterElement : IBufferElementData
{
    public SpawnMasterId Id;
    public float SpawnInterval;
    public float InitialEnemySpawnDelay;
    public float MinSpawnDistance;
    public float MaxSpawnDistance;

    public static SpawnMasterElement FromMaster(SpawnMasterData master)
    {
        return new SpawnMasterElement
        {
            Id = master.Id,
            SpawnInterval = master.SpawnInterval,
            InitialEnemySpawnDelay = master.InitialEnemySpawnDelay,
            MinSpawnDistance = master.MinSpawnDistance,
            MaxSpawnDistance = master.MaxSpawnDistance,
        };
    }

    public SpawnMasterData ToRuntimeMaster()
    {
        return new SpawnMasterData
        {
            Id = Id,
            SpawnInterval = SpawnInterval,
            InitialEnemySpawnDelay = InitialEnemySpawnDelay,
            MinSpawnDistance = MinSpawnDistance,
            MaxSpawnDistance = MaxSpawnDistance,
        };
    }
}
