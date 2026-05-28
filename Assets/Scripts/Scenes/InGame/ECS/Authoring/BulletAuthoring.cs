using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            
            // デフォルトでは、コンポーネントはゼロで初期化されます。
            // したがって、CannonBall の Velocity フィールドは float3.zero になります。
            AddComponent<Bullet>(entity);
            AddComponent<URPMaterialPropertyBaseColor>(entity);
        }
    }
}

public struct Bullet : IComponentData
{
    public Entity Shooter;  // 発射者のEntity
    public float3 Velocity;
}
