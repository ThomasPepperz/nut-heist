using NutHeist.Environment;
using NutHeist.Player;
using TMPro;
using UnityEngine;

namespace NutHeist.UI
{
    /// <summary>
    /// Watches InteractionManager.FocusedInteractable and writes "[E] Open"
    /// (or whatever GetPrompt() returns) to a TMP label.
    ///
    /// Setup: create a TextMeshProUGUI in your HUD canvas, attach this component
    /// to any scene object, and wire the label reference. InteractionManager
    /// is found automatically if not assigned.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] InteractionManager interactionManager;

        void Update()
        {
            interactionManager ??= FindFirstObjectByType<InteractionManager>();
            if (!label) return;

            Interactable focus = interactionManager != null
                ? interactionManager.FocusedInteractable
                : null;

            // Also suppress the prompt while the player is carrying something —
            // the only available action is Drop, handled by InteractionManager.
            bool carrying = false;
            if (focus != null)
            {
                var squirrel = FindFirstObjectByType<SquirrelController>();
                if (squirrel != null)
                {
                    var carry = squirrel.GetComponent<CarrySystem>();
                    carrying = carry != null && carry.IsCarrying;
                }
            }

            if (carrying)
            {
                label.text = "[E] Drop";
                return;
            }

            if (focus == null || string.IsNullOrEmpty(focus.GetPrompt()))
            {
                label.text = string.Empty;
                return;
            }

            label.text = $"[E] {focus.GetPrompt()}";
        }
    }
}
