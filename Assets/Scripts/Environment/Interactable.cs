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

    /// <summary>Base metadata for tiered authoring (Section 10).</summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField]
        InteractionTier[] tiers =
        {
            InteractionTier.PassivePhysics
        };

        [SerializeField] float massKg = 5f;

        public float MassKg => massKg;

        public bool HasTier(InteractionTier tier)
        {
            if (tiers == null || tiers.Length == 0)
            {
                return false;
            }

            foreach (InteractionTier flag in tiers)
            {
                if (flag == tier)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
