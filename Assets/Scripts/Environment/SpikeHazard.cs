using NutHeist.Audio;
using NutHeist.Player;
using UnityEngine;

namespace NutHeist.Environment
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpikeHazard : MonoBehaviour
    {
        [SerializeField]
        float knockStrength = 6f;

        [SerializeField]
        float stunDuration = 0.5f;

        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (!squirrelLocomotor)
            {
                return;
            }

            Vector3 awayPlanar = squirrelLocomotor.transform.position - transform.position;
            awayPlanar.y = 0f;
            Vector3 impulse = awayPlanar.sqrMagnitude > Mathf.Epsilon
                ? awayPlanar.normalized * knockStrength
                : squirrelLocomotor.transform.forward * -knockStrength;
            squirrelLocomotor.ApplyKnockback(impulse + Vector3.up * 2.5f, stunDuration);
            SoundManager.Resolve()?.PlayLandingHeavy(0.45f);
        }
    }
}
