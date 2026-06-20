using Unity.Entities;

/// <summary>
/// 操作対象の ActorBody を識別するタグコンポーネント。
/// 敵移動システムはこのタグを持つエンティティを対象外にする。
/// </summary>
public struct Player : IComponentData
{
}
