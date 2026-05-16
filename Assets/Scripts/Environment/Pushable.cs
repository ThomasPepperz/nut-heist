using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>Physics-friendly prop that can absorb light forces from scripted events.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Pushable : Interactable
    {
        Rigidbody _body;

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

#if UNITY_EDITOR
        void Reset()
        {
            if (!TryGetComponent(out Rigidbody body))
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.mass = Mathf.Clamp(MassKg, 0.1f, 500f);
        }
#endif

        /// <summary>Called by scripted interactions.</summary>
        public void Boost(Vector3 worldImpulse)
        {
            _body ??= GetComponent<Rigidbody>();
            if (!_body.isKinematic)
            {
                _body.AddForce(worldImpulse, ForceMode.Impulse);
            }
        }
    }
}
