using Unity.Entities;
using UnityEngine;

public class TankAuthoring : MonoBehaviour
{
    public GameObject Turret;
    public GameObject Canon;
    
    class Baker: Baker<TankAuthoring>
    {
        public override void Bake(TankAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Tank
            {
                Turret = GetEntity(authoring.Turret, TransformUsageFlags.Dynamic),
                Canon = GetEntity(authoring.Canon, TransformUsageFlags.Dynamic),
            });
            AddBuffer<TankDamage>(entity);
        }
    }
}

public struct Tank: IComponentData
{
    public Entity Turret;
    public Entity Canon;
}　

public struct TankDamage: IBufferElementData
{
    public int Damage;
    public Entity Attacker;
}　
