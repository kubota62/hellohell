using Unity.Entities;
using Unity.Rendering;

/// <summary>
/// 親Actorの色を、砲塔や砲身などの子描画Entityへ一度だけ同期するシステム。
/// スポーン直後の見た目に関わる初期化なので、ECB遅延に頼らずその場で反映する。
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SyncColorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (color, targets, entity) in
                 SystemAPI.Query<RefRO<URPMaterialPropertyBaseColor>, DynamicBuffer<SyncColor>>()
                     .WithEntityAccess())
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var targetEntity = targets[i].SyncTarget;
                if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(targetEntity))
                {
                    ecb.SetComponent(targetEntity, color.ValueRO);
                }
            }

            ecb.RemoveComponent<SyncColor>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
