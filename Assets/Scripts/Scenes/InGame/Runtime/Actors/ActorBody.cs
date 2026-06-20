using Unity.Entities;

/// <summary>
/// 砲塔と砲身を持つ操作主体の共通ボディ。
/// Player か Enemy かは別タグで表し、見た目や攻撃の土台だけをここに集約する。
/// </summary>
public struct ActorBody : IComponentData
{
    public Entity Turret;
    public Entity Canon;
}
