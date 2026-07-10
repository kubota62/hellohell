using Unity.Entities;

/// <summary>
/// Player/Core の共通タグ。
/// 操作対象の ActorBody を識別する。
/// 敵移動システムはこのタグを持つエンティティを対象外にする。
/// </summary>
public struct Player : IComponentData
{
}
