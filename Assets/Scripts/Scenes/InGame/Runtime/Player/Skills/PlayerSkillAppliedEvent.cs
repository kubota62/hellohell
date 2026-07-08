using Unity.Entities;

/// <summary>
/// Playerへスキル強化が適用された時に一度だけ発行されるイベント。
/// 取得演出、SE、HUDログはこのイベントを読むだけで後から追加できる。
/// </summary>
public struct PlayerSkillAppliedEvent : IComponentData
{
    public Entity Player;
    public PlayerSkillMasterId SkillId;
    public PlayerSkillKind Kind;
    public int AddedLevel;
    public int NewSkillLevel;
}
