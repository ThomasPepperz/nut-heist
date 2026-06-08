using UnityEngine;

namespace NutHeist.Core
{
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameplayTags.Player))
                GameLoop.Instance?.RegisterCheckpoint(transform.position, transform.rotation);
        }
    }
}
