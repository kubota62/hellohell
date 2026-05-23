using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial class CameraFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CameraTarget>())
        {
            var targetPos = transform.ValueRO.Position;
            mainCamera.transform.position = new Vector3(targetPos.x, targetPos.y + 10f, targetPos.z - 10f);
            mainCamera.transform.LookAt(new Vector3(targetPos.x, targetPos.y, targetPos.z));
            break; // 最初のターゲットのみ追跡
        }
    }
}
