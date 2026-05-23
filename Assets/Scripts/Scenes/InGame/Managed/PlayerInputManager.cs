using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var movement = new float2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)
        );

        var isFire = keyboard.spaceKey.isPressed;
        
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(typeof(PlayerInput));

        if (query.IsEmpty)
        {
            var entity = entityManager.CreateEntity(typeof(PlayerInput));
            entityManager.SetComponentData(
                entity, 
                new PlayerInput
                {
                    IsFire = isFire,
                    Movement = movement,
                });
        }
        else
        {
            query.SetSingleton(
                new PlayerInput
                {
                    IsFire = isFire,
                    Movement = movement,
                });
        }
    }
}
