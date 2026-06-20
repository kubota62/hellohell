using Unity.Entities;

/// <summary>
/// 戦闘、ターゲット選択、命中フィルタで使う安定した所属ID。
/// アーキタイプの断片化を避けるため、ホットパスの Actor では共通コンポーネントとして持たせる。
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
