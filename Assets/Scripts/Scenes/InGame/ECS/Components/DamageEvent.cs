using Unity.Entities;

public struct DamageEvent: IBufferElementData
{
    public int Damage;
    public Entity Attacker;
}　
