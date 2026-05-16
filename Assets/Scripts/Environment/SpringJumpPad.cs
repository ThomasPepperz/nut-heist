using NutHeist.Audio;
using NutHeist.Player;
using UnityEngine;

namespace NutHeist.Environment
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpringJumpPad : MonoBehaviour
    {
        [SerializeField]
        float launchImpulse = 20f;

        void Reset()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (!squirrelLocomotor)
            {
                return;
            }

            squirrelLocomotor.AddVerticalBurst(launchImpulse);
            SoundManager.Resolve()?.PlayLandingHeavy(0.4f);
        }
    }
}
