using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// ダメージを受ける Actor が共有する体力状態。
/// ダメージ関連タグの追加削除ではなく、各システムは Current を更新する。
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
