using System.Collections.Generic;
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
            var tankEntity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var turretEntity = GetEntity(authoring.Turret, TransformUsageFlags.Dynamic);
            var canonEntity = GetEntity(authoring.Canon, TransformUsageFlags.Dynamic);
            AddComponent(tankEntity, new Tank
            {
                Turret = turretEntity,
                Canon = canonEntity,
            });
            AddBuffer<DamageEvent>(tankEntity);
            
            // DynamicBufferを自分自身のEntityに追加
            var buffer = AddBuffer<SyncColor>(tankEntity);
            buffer.Add(new SyncColor{SyncTarget = turretEntity});
            buffer.Add(new SyncColor{SyncTarget = canonEntity});
        }
    }
}

public struct Tank: IComponentData
{
    public Entity Turret;
    public Entity Canon;
}

public struct SyncColor: IBufferElementData
{
    public Entity SyncTarget;
}