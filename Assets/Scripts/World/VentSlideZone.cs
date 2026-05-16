using NutHeist.Player;
using UnityEngine;

namespace NutHeist.World
{
    /// <summary>Speed-up strip that amplifies planar vent traversal (condensation slick).</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class VentSlideZone : MonoBehaviour
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

            squirrelLocomotor.VentTraversalScaler = Mathf.Clamp(
                Mathf.Max(VentSystem.Instance.condensationSlide / 5f, 1.05f),
                1.05f,
                3f);
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
