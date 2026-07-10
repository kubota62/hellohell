using Unity.Entities;

/// <summary>
/// Player/Core/Skills の共通イベント。
/// Playerへスキル強化が適用された時に一度だけ発行される。
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
