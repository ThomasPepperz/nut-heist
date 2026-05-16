using UnityEngine;

namespace NutHeist.Environment
{
    public sealed class Destructible : Interactable
    {
        [SerializeField]
        float breakVelocity = 14f;

        void OnCollisionEnter(Collision collision)
        {
            if (!HasTier(InteractionTier.Destructible))
            {
                return;
            }

            if (collision.relativeVelocity.magnitude >= breakVelocity)
            {
                Destroy(gameObject);
            }
        }
    }
}
