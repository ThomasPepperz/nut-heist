using NutHeist.AI;
using UnityEngine;

namespace NutHeist.Environment
{
    // Attach to a thrown or dropped prop. On first hard collision it emits a noise
    // event through GuardAlertNetwork so guards investigate the landing site.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ImpactNoise : MonoBehaviour
    {
        [SerializeField] float noiseRadius = 14f;
        [SerializeField] float minImpactSpeed = 1.5f;

        bool armed = true;

        public void Arm() => armed = true;

        void OnCollisionEnter(Collision col)
        {
            if (!armed) return;
            if (col.relativeVelocity.magnitude < minImpactSpeed) return;
            armed = false;
            NoiseEmitter.EmitAt(transform.position, noiseRadius);
        }
    }
}
