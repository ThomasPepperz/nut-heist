using NutHeist.Player;
using UnityEngine;

namespace NutHeist.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class VentAirflowVolume : MonoBehaviour
    {
        [SerializeField] Vector3 airflowDirectionLocal = Vector3.forward;
        SquirrelController cachedSquirrel;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            cachedSquirrel = other.GetComponentInParent<SquirrelController>();
            if (!cachedSquirrel || !VentSystem.Instance)
            {
                return;
            }

            Vector3 worldDir = transform.TransformDirection(airflowDirectionLocal.normalized);
            cachedSquirrel.ExternalImpulse += worldDir * (VentSystem.Instance.airflowForce * Time.deltaTime);
        }
    }
}
