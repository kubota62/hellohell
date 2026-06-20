using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Shared health state for damageable actors.
/// Systems should mutate Current instead of adding/removing damage-related tags.
/// </summary>
public struct Health : IComponentData
{
    public int Max;
    public int Current;

    public bool IsAlive => Current > 0;

    public static Health FromMax(int max)
    {
        max = math.max(1, max);
        return new Health
        {
            Max = max,
            Current = max
        };
    }
}
