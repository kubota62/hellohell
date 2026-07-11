using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public struct SpatialHashSnapshot
{
    [ReadOnly] public NativeParallelMultiHashMap<int, int> Hash;
    [ReadOnly] public NativeArray<Entity> Entities;
    [ReadOnly] public NativeArray<LocalTransform> Transforms;
    [ReadOnly] public NativeArray<Team> Teams;
    [ReadOnly] public NativeArray<Hitbox> Hitboxes;
}

public struct SpatialHashEntry
{
    public Entity Entity;
    public float3 Position;
    public float Radius;
    public TeamId Team;
}

/// <summary>
/// XZ 平面上のワールド座標を決定的なグリッドセルへ変換する Burst 向けヘルパー。
/// </summary>
public static class SpatialHashUtility
{
    const int XPrime = 73856093;
    const int ZPrime = 19349663;

    public static int2 GetCell(float3 position, float cellSize)
    {
        var safeCellSize = math.max(0.001f, cellSize);
        return new int2(
            (int)math.floor(position.x / safeCellSize),
            (int)math.floor(position.z / safeCellSize));
    }

    public static int GetHash(int2 cell)
    {
        return (cell.x * XPrime) ^ (cell.y * ZPrime);
    }

    public static int GetHash(float3 position, float cellSize)
    {
        return GetHash(GetCell(position, cellSize));
    }

    /// <summary>
    /// 攻撃判定で共通利用する、現在フレームのターゲット一覧と空間ハッシュを構築する。
    /// 返却するNativeコンテナはWorldUpdateAllocatorに属するため、呼び出し側でDisposeしない。
    /// </summary>
    public static SpatialHashSnapshot BuildTargetSnapshot(
        EntityQuery targetQuery,
        ref SystemState state,
        float cellSize)
    {
        var entities = targetQuery.ToEntityArray(state.WorldUpdateAllocator);
        var transforms = targetQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
        var teams = targetQuery.ToComponentDataArray<Team>(state.WorldUpdateAllocator);
        var hitboxes = targetQuery.ToComponentDataArray<Hitbox>(state.WorldUpdateAllocator);
        var hash = new NativeParallelMultiHashMap<int, int>(
            math.max(1, entities.Length),
            state.WorldUpdateAllocator);

        for (var i = 0; i < entities.Length; i++)
        {
            hash.Add(GetHash(transforms[i].Position, cellSize), i);
        }

        return new SpatialHashSnapshot
        {
            Hash = hash,
            Entities = entities,
            Transforms = transforms,
            Teams = teams,
            Hitboxes = hitboxes,
        };
    }
}
