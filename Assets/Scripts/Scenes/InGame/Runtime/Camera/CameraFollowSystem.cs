using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// CameraTarget を持つ Actor を追従するメインカメラ制御。
/// Entity の移動が終わった後に GameObject の Camera を更新するため、PresentationSystemGroup で実行する。
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{
    private const float CameraHeight = 18f;
    private const float CameraBackOffset = 8f;

    protected override void OnCreate()
    {
        RequireForUpdate<CameraTarget>();
    }

    protected override void OnUpdate()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CameraTarget>())
        {
            var targetPos = transform.ValueRO.Position;
            mainCamera.transform.position = new Vector3(
                targetPos.x,
                targetPos.y + CameraHeight,
                targetPos.z - CameraBackOffset);
            mainCamera.transform.LookAt(new Vector3(targetPos.x, targetPos.y, targetPos.z));
            break;
        }
    }
}
