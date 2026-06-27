using Unity.Entities;

/// <summary>
/// 現在ゲーム処理に参加しているプール対象エンティティを示す Enableable マーカー。
/// 頻繁に再利用するエンティティでは、構造変更よりこの有効状態の切り替えを優先する。
/// </summary>
public struct GameplayActive : IComponentData, IEnableableComponent
{
}
