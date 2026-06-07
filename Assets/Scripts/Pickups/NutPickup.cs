using NutHeist.Core;
using NutHeist.Environment;
using NutHeist.Player;
using NutHeist.Progress;
using UnityEngine;

namespace NutHeist.Pickups
{
    /// <summary>
    /// Collected in two ways: walk into the trigger sphere (auto-grab),
    /// or press E when focused by InteractionManager (explicit steal).
    /// Both paths call the same Collect() so the counter is never double-hit.
    /// </summary>
    public sealed class NutPickup : Interactable
    {
        bool collected;

        // Auto-collect: trigger sphere on the prefab, no button press needed.
        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameplayTags.Player)) return;
            Collect();
        }

        public override string GetPrompt() => "Grab nut";

        // Explicit collect: player presses E while this nut is focused.
        public override void Activate(SquirrelController squirrel)
        {
            Collect();
        }

        void Collect()
        {
            if (collected) return;
            collected = true;
            NutProgress.Instance?.CollectNut();
            Destroy(gameObject);
        }
    }
}
