using Unity.Entities;
using UnityEngine;

public class TankAuthoring : MonoBehaviour
{
    public GameObject Turret;
    public GameObject Canon;
    public int MaxHealth = 100;
    public float HitRadius = 2f;
    
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
            AddComponent(tankEntity, new Team { Value = TeamId.Neutral });
            AddComponent(tankEntity, Health.FromMax(authoring.MaxHealth));
            AddComponent(tankEntity, new Hitbox { Radius = authoring.HitRadius });
            AddComponent<SpatialHashTarget>(tankEntity);
            AddComponent<GameplayActive>(tankEntity);
            AddBuffer<DamageEvent>(tankEntity);
            
            // DynamicBufferを自分自身のEntityに追加
            var buffer = AddBuffer<SyncColor>(tankEntity);
            buffer.Add(new SyncColor{SyncTarget = turretEntity});
            buffer.Add(new SyncColor{SyncTarget = canonEntity});
        }
    }
}
