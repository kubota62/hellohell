using Unity.Entities;

public struct Tank : IComponentData
{
    public Entity Turret;
    public Entity Canon;
}
