using NutHeist.Player;
using NutHeist.Progress;
using UnityEngine;

namespace NutHeist.Pickups
{
    /// <summary>Session-only collectible the pickup sensor listens for.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class NutPickup : MonoBehaviour
    {
        void Reset()
        {
            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.06f;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<PickupInteractor>())
            {
                return;
            }

            NutProgress.Instance?.CollectNut();

            Destroy(gameObject);
        }
    }
}
