using DG.Tweening;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unity Input System のキーボード状態を ECS の PlayerInput コンポーネントへ橋渡しする。
/// ECS 側の移動や射撃システムは、この入力エンティティを毎フレーム参照する。
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    EntityManager entityManager;
    private Entity entity;
    private Camera mainCamera;

    private void Start()
    {
        DOTween.Init();
        DOTween.SetTweensCapacity(10000, 5000);

        // 現在の入力状態だけを保持する ECS エンティティを作る。
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = entityManager.CreateEntity(typeof(PlayerInput));
        mainCamera = Camera.main;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        var movement = new float2(
            keyboard == null ? 0f : (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            keyboard == null ? 0f : (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)
        );
        movement = math.normalizesafe(movement);

        var isFire = keyboard != null && keyboard.spaceKey.isPressed;
        var hasAimPosition = TryGetAimWorldPosition(mouse, out var aimWorldPosition);

        // 最新のキーボード状態を PlayerInput に同期する。
        entityManager.SetComponentData(entity, new PlayerInput
        {
            IsFire = isFire,
            HasAimPosition = hasAimPosition,
            Movement = movement,
            AimWorldPosition = aimWorldPosition,
        });
    }

    private bool TryGetAimWorldPosition(Mouse mouse, out float3 aimWorldPosition)
    {
        aimWorldPosition = float3.zero;
        if (mouse == null)
        {
            return false;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }
        }

        var ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        var groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out var distance))
        {
            return false;
        }

        aimWorldPosition = ray.GetPoint(distance);
        return true;
    }
}
