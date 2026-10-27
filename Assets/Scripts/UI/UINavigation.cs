using UnityEngine;
using UnityEngine.InputSystem;

// Whether menus are being worked with keys or a gamepad (show which button is picked) or the mouse (don't:
// hovering already shows it). Switches to keys on any key or gamepad press and back to the mouse when it
// moves or clicks. Each menu calls Reset when it opens, so keys pressed while playing (walking with WASD)
// don't count: nothing is picked until a key is pressed in the menu.
public static class UINavigation
{
    private static bool usingKeys;
    private static int checkedFrame = -1;

    public static bool UsingKeys
    {
        get
        {
            if (checkedFrame != Time.frameCount)
            {
                checkedFrame = Time.frameCount;
                Check();
            }
            return usingKeys;
        }
    }

    public static void Reset()
    {
        usingKeys = false;
        checkedFrame = Time.frameCount; // this frame's presses opened the menu
    }

    private static void Check()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 1f || mouse.leftButton.wasPressedThisFrame))
        {
            usingKeys = false;
            return;
        }

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            usingKeys = true;

        Gamepad pad = Gamepad.current;
        if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.dpad.ReadValue().sqrMagnitude > 0.25f ||
                            pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame))
            usingKeys = true;
    }
}
