using NutHeist.Player;
using UnityEngine;

namespace NutHeist.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class VentSpiderWebZone : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (!squirrelLocomotor || !VentSystem.Instance)
            {
                return;
            }

            float scaler = Mathf.Max(1f, VentSystem.Instance.spiderWebSlow);
            squirrelLocomotor.VentTraversalScaler = Mathf.Min(squirrelLocomotor.VentTraversalScaler, 1f / scaler);
        }

        void OnTriggerExit(Collider other)
        {
            SquirrelController squirrelLocomotor = other.GetComponentInParent<SquirrelController>();
            if (squirrelLocomotor)
            {
                squirrelLocomotor.VentTraversalScaler = 1f;
            }
        }
    }
}
