using UnityEngine;
using UnityEngine.InputSystem;

namespace NutHeist.Player
{
    /// <summary>Keyboard-first reads; swap implementation later for touch without touching gameplay code.</summary>
    public sealed class SquirrelInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }

        void Update()
        {
            JumpPressedThisFrame = false;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
            {
                Move = Vector2.zero;
                SprintHeld = false;
                JumpHeld = false;
                Look = Vector2.zero;
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Move = new Vector2(x, z);
            if (Move.sqrMagnitude > 1f)
            {
                Move.Normalize();
            }

            SprintHeld = keyboard.leftShiftKey.isPressed;
            JumpHeld = keyboard.spaceKey.isPressed;
            JumpPressedThisFrame = keyboard.spaceKey.wasPressedThisFrame;

            if (mouse != null)
            {
                Look = mouse.delta.ReadValue();
            }
        }
    }
}
