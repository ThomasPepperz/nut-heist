using NutHeist.Player;
using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>Focal point for overlapping interactables.</summary>
    public sealed class InteractionManager : MonoBehaviour
    {
        [SerializeField] SquirrelController squirrel;
        [SerializeField] float scanRadius = 2.5f;
        readonly Collider[] _overlap = new Collider[96];

        void Update()
        {
            squirrel ??= FindFirstObjectByType<SquirrelController>();
            if (!squirrel)
            {
                return;
            }

            int hits = Physics.OverlapSphereNonAlloc(squirrel.transform.position, scanRadius, _overlap, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits; i++)
            {
                if (!_overlap[i])
                {
                    continue;
                }

                _overlap[i].GetComponentInParent<Interactable>();
            }
        }
    }
}
