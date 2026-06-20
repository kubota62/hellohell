using Unity.Entities;
using UnityEngine;

/// <summary>
/// Player と Enemy が共有する ActorBody プレハブを ECS に変換する Authoring。
/// 敵の種類は EnemyTypeId や将来の EnemyDefinition で差し替える。
/// </summary>
public class ActorBodyAuthoring : MonoBehaviour
{
    public GameObject Turret;
    public GameObject Canon;
    public int MaxHealth = 100;
    public float HitRadius = 2f;
    
    class Baker: Baker<ActorBodyAuthoring>
    {
        public override void Bake(ActorBodyAuthoring authoring)
        {
            var actorEntity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var turretEntity = GetEntity(authoring.Turret, TransformUsageFlags.Dynamic);
            var canonEntity = GetEntity(authoring.Canon, TransformUsageFlags.Dynamic);
            AddComponent(actorEntity, new ActorBody
            {
                Turret = turretEntity,
                Canon = canonEntity,
            });
            AddComponent(actorEntity, new Team { Value = TeamId.Neutral });
            AddComponent(actorEntity, Health.FromMax(authoring.MaxHealth));
            AddComponent(actorEntity, new Hitbox { Radius = authoring.HitRadius });
            AddComponent<SpatialHashTarget>(actorEntity);
            AddComponent<GameplayActive>(actorEntity);
            AddBuffer<DamageEvent>(actorEntity);
            
            // 親ボディの色を砲塔と砲身にも同期する。
            var buffer = AddBuffer<SyncColor>(actorEntity);
            buffer.Add(new SyncColor{SyncTarget = turretEntity});
            buffer.Add(new SyncColor{SyncTarget = canonEntity});
        }
    }
}
