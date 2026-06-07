using NutHeist.Player;
using UnityEngine;

namespace NutHeist.Environment
{
    public enum InteractionTier
    {
        PassivePhysics,
        ClimbableTier,
        Pushable,
        Openable,
        Destructible,
        Triggerable
    }

    /// <summary>
    /// Base class for every world object the squirrel can interact with.
    /// Subclasses override GetPrompt() and Activate() to define their verb.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField]
        InteractionTier[] tiers = { InteractionTier.PassivePhysics };

        [SerializeField] float massKg = 5f;

        public float MassKg => massKg;

        public bool HasTier(InteractionTier tier)
        {
            if (tiers == null || tiers.Length == 0) return false;
            foreach (InteractionTier flag in tiers)
                if (flag == tier) return true;
            return false;
        }

        // Label shown in the HUD prompt, e.g. "Open door" or "Grab nut".
        // Return empty string to suppress the prompt for this object.
        public virtual string GetPrompt() => "Interact";

        // Called when the player presses Interact while this object is focused.
        public virtual void Activate(SquirrelController squirrel) { }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
