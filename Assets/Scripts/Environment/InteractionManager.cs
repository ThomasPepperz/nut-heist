using NutHeist.Player;
using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>
    /// Each frame scores all nearby Interactables, focuses the best one
    /// (closest inside the player's forward arc), and fires Activate on E.
    /// Expose FocusedInteractable for the HUD prompt.
    /// </summary>
    public sealed class InteractionManager : MonoBehaviour
    {
        [SerializeField] SquirrelController squirrel;
        [SerializeField] float scanRadius = 2.5f;
        // Objects outside this half-angle from the squirrel's forward are ignored.
        [SerializeField] float arcDegrees = 70f;

        readonly Collider[] _overlap = new Collider[96];

        public Interactable FocusedInteractable { get; private set; }

        void Update()
        {
            squirrel ??= FindFirstObjectByType<SquirrelController>();
            if (!squirrel) return;

            FocusedInteractable = FindBest();

            if (squirrel.InputReader.InteractPressedThisFrame)
            {
                // While carrying, E always drops — don't also fire Activate.
                var carry = squirrel.GetComponent<CarrySystem>();
                if (carry != null && carry.IsCarrying)
                {
                    carry.Drop();
                }
                else if (FocusedInteractable != null)
                {
                    FocusedInteractable.Activate(squirrel);
                }
            }
        }

        Interactable FindBest()
        {
            int hits = Physics.OverlapSphereNonAlloc(
                squirrel.transform.position,
                scanRadius,
                _overlap,
                ~0,
                QueryTriggerInteraction.Collide);

            Interactable best = null;
            float bestScore = float.MaxValue;
            Vector3 forward = squirrel.transform.forward;
            float cosArc = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);

            for (int i = 0; i < hits; i++)
            {
                if (!_overlap[i]) continue;
                var interactable = _overlap[i].GetComponentInParent<Interactable>();
                if (!interactable) continue;

                Vector3 toTarget = interactable.transform.position - squirrel.transform.position;
                float dist = toTarget.magnitude;
                if (dist < 0.01f) continue;

                // Must fall inside the forward arc.
                float dot = Vector3.Dot(forward, toTarget / dist);
                if (dot < cosArc) continue;

                // Score favours items that are close AND centred in the look direction.
                float score = dist * (1f - dot * 0.4f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = interactable;
                }
            }

            return best;
        }
    }
}
