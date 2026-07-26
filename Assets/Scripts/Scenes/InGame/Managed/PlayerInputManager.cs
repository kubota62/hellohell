using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity Input Systemの状態を、ECSのPlayerInput Singletonへ同期する。
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    private World world;
    private EntityManager entityManager;
    private Entity inputEntity;
    private EntityQuery runStateQuery;
    private Camera mainCamera;
    private bool autoAttackEnabled;
    private bool hasInputEntity;
    private bool hasRunStateQuery;
    private uint activeAttackMask = AttackMasterIdUtility.CreatePlayerDefaultMask();

    private void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            enabled = false;
            return;
        }

        entityManager = world.EntityManager;
        inputEntity = entityManager.CreateEntity(typeof(PlayerInput));
        runStateQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RunState>());
        hasRunStateQuery = true;
        hasInputEntity = true;
        mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (world != null &&
            world.IsCreated &&
            hasRunStateQuery)
        {
            runStateQuery.Dispose();
            hasRunStateQuery = false;
        }

        if (hasInputEntity &&
            world != null &&
            world.IsCreated &&
            entityManager.Exists(inputEntity))
        {
            entityManager.DestroyEntity(inputEntity);
            hasInputEntity = false;
        }
    }

    private void Update()
    {
        if (!hasInputEntity || world == null || !world.IsCreated)
        {
            return;
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (IsGameOver())
        {
            entityManager.SetComponentData(inputEntity, new PlayerInput
            {
                AutoAttackEnabled = autoAttackEnabled,
                ActiveAttackMask = activeAttackMask,
            });

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                enabled = false;
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.buildIndex >= 0)
                {
                    SceneManager.LoadScene(activeScene.buildIndex);
                }
                else
                {
                    SceneManager.LoadScene(activeScene.name);
                }
            }

            return;
        }

        UpdateAttackToggles(keyboard);
        var movement = ReadMovement(keyboard);
        var hasAimPosition = TryGetAimWorldPosition(
            mouse,
            out var aimWorldPosition);

        entityManager.SetComponentData(inputEntity, new PlayerInput
        {
            IsFire = keyboard != null && keyboard.spaceKey.isPressed,
            HasAimPosition = hasAimPosition,
            AutoAttackEnabled = autoAttackEnabled,
            Movement = movement,
            AimWorldPosition = aimWorldPosition,
            ActiveAttackMask = activeAttackMask,
        });
    }

    private bool IsGameOver()
    {
        return !runStateQuery.IsEmptyIgnoreFilter &&
            runStateQuery.GetSingleton<RunState>().IsGameOver != 0;
    }

    private void UpdateAttackToggles(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.tKey.wasPressedThisFrame)
        {
            autoAttackEnabled = !autoAttackEnabled;
        }

        ToggleAttackIfPressed(
            keyboard.digit1Key.wasPressedThisFrame,
            AttackMasterId.BasicMeleeArc);
        ToggleAttackIfPressed(
            keyboard.digit2Key.wasPressedThisFrame,
            AttackMasterId.RapidBolt);
        ToggleAttackIfPressed(
            keyboard.digit3Key.wasPressedThisFrame,
            AttackMasterId.PiercingLance);
        ToggleAttackIfPressed(
            keyboard.digit4Key.wasPressedThisFrame,
            AttackMasterId.ExplosiveOrb);
    }

    private void ToggleAttackIfPressed(
        bool wasPressed,
        AttackMasterId attackMasterId)
    {
        if (wasPressed)
        {
            activeAttackMask ^= attackMasterId.ToMask();
        }
    }

    private static float2 ReadMovement(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return float2.zero;
        }

        var movement = new float2(
            ReadAxis(keyboard.aKey.isPressed, keyboard.dKey.isPressed),
            ReadAxis(keyboard.sKey.isPressed, keyboard.wKey.isPressed));
        return math.normalizesafe(movement);
    }

    private static float ReadAxis(bool negative, bool positive)
    {
        return (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }

    private bool TryGetAimWorldPosition(
        Mouse mouse,
        out float3 aimWorldPosition)
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
