using Unity.Entities;
using Unity.Mathematics;

public struct SpatialHashEntry
{
    public Entity Entity;
    public float3 Position;
    public float Radius;
    public TeamId Team;
}

/// <summary>
/// Burst-friendly helpers for mapping XZ world positions into deterministic grid cells.
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
}
