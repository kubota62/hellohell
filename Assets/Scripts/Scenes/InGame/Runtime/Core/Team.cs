using Unity.Entities;

/// <summary>
/// Stable team ids used by combat, target selection, and hit filtering.
/// Keep this component common across hot-path actors to avoid archetype churn.
/// </summary>
public enum TeamId : byte
{
    Neutral = 0,
    Player = 1,
    Enemy = 2
}

public struct Team : IComponentData
{
    public TeamId Value;
}

public static class TeamUtility
{
    public static bool AreHostile(TeamId a, TeamId b)
    {
        return a != TeamId.Neutral && b != TeamId.Neutral && a != b;
    }
}
