using NutHeist.Player;
using UnityEngine;

namespace NutHeist.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class VentTraversalVolume : MonoBehaviour
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
                squirrelLocomotor.NotifyVentEnter();
            }
        }

        void OnTriggerExit(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (squirrelLocomotor)
            {
                squirrelLocomotor.NotifyVentExit();
                squirrelLocomotor.VentTraversalScaler = 1f;
            }
        }
    }
}
