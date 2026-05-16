using NutHeist.Player;
using UnityEngine;

namespace NutHeist.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class SwimmingVolume : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (squirrelLocomotor)
            {
                squirrelLocomotor.NotifyWaterEnter();
            }
        }

        void OnTriggerExit(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (squirrelLocomotor)
            {
                squirrelLocomotor.NotifyWaterExit();
            }
        }
    }
}
