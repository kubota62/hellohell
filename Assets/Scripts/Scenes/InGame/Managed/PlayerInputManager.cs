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

    private void Start()
    {
        DOTween.Init();
        DOTween.SetTweensCapacity(10000, 5000);

        // 現在の入力状態だけを保持する ECS エンティティを作る。
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = entityManager.CreateEntity(typeof(PlayerInput));
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var movement = new float2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)
        );
        movement = math.normalizesafe(movement);

        var isFire = keyboard.spaceKey.isPressed;

        // 最新のキーボード状態を PlayerInput に同期する。
        entityManager.SetComponentData(entity, new PlayerInput
        {
            IsFire = isFire,
            Movement = movement,
        });
    }
}
