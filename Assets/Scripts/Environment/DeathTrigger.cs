using NutHeist.Core;
using UnityEngine;

namespace NutHeist.Environment
{
    [RequireComponent(typeof(Collider))]
    public sealed class DeathTrigger : MonoBehaviour
    {
        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameplayTags.Player))
                GameLoop.Instance?.NotifyCaught();
        }
    }
}
