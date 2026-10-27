using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private PlayerInputActions playerInputActions;
    private InputAction totemAction;

    // Weapon switching: Q / right shoulder for the next weapon, left shoulder for the one before,
    // 1-3 straight to that slot (0-based)
    public event Action<int> OnSwitchWeapon;
    public event Action<int> OnSelectWeaponSlot;
    // E / gamepad north: plant a carried Crimson Weed seed
    public event Action OnInteract;
    // T / gamepad west: start using the Totem (it's held down while channelling)
    public event Action OnTotem;
    public bool IsTotemHeld => totemAction != null && totemAction.IsPressed();

    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();

        // Looked up by name so this doesn't depend on the generated class being rebuilt
        InputActionAsset asset = playerInputActions.asset;
        Listen(asset, "NextWeapon", () => OnSwitchWeapon?.Invoke(1));
        Listen(asset, "PreviousWeapon", () => OnSwitchWeapon?.Invoke(-1));
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            Listen(asset, "WeaponSlot" + (i + 1), () => OnSelectWeaponSlot?.Invoke(slot));
        }
        Listen(asset, "Interact", () => OnInteract?.Invoke());
        Listen(asset, "Totem", () => OnTotem?.Invoke());
        totemAction = asset.FindAction("Totem");
    }

    void OnDestroy()
    {
        playerInputActions.Player.Disable();
        playerInputActions.Dispose();
    }

    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();
        inputVector = inputVector.normalized;
        return inputVector;
    }

    private static void Listen(InputActionAsset asset, string actionName, Action pressed)
    {
        InputAction action = asset.FindAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"GameInput: no '{actionName}' action in PlayerInputActions.");
            return;
        }
        action.performed += _ => pressed();
    }
}
