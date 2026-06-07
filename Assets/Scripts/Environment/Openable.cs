using NutHeist.Player;
using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>
    /// Press E to open/close a door, vent grate, or hatch.
    /// Drives localRotation with a SmoothStep lerp — no tween library needed.
    /// Set openRotationOffset in the Inspector (e.g. (0, 90, 0) for a door
    /// that swings 90° on the Y axis).
    /// </summary>
    public sealed class Openable : Interactable
    {
        [SerializeField] Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
        [SerializeField] float animSeconds = 0.35f;

        bool open;
        Quaternion closedRot;
        Quaternion openRot;
        // Normalised animation progress, 0→1, reset to 0 on each toggle.
        float animT = 1f;

        void Awake()
        {
            closedRot = transform.localRotation;
            openRot = closedRot * Quaternion.Euler(openRotationOffset);
        }

        public override string GetPrompt() => open ? "Close" : "Open";

        public override void Activate(SquirrelController squirrel)
        {
            open = !open;
            animT = 0f;
        }

        void Update()
        {
            if (animT >= 1f) return;
            animT = Mathf.Min(animT + Time.deltaTime / animSeconds, 1f);
            // SmoothStep eases in and out so it doesn't pop or overshoot.
            float t = Mathf.SmoothStep(0f, 1f, animT);
            transform.localRotation = Quaternion.Slerp(
                open ? closedRot : openRot,
                open ? openRot : closedRot,
                t);
        }
    }
}
