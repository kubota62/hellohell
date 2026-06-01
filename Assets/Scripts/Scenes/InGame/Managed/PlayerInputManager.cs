using System;
using DG.Tweening;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    EntityManager entityManager;
    private Entity entity;

    private void Start()
    {
        DOTween.Init();
        DOTween.SetTweensCapacity(10000, 5000);

        // PlayerInputを持ったentityを作成
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

        // entityのPlayerInputを更新
        entityManager.SetComponentData(entity, new PlayerInput
        {
            IsFire = isFire,
            Movement = movement,
        });
    }
}