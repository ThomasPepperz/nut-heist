using NutHeist.Core;
using UnityEngine;

namespace NutHeist.Environment
{
    // Place inside a bush, dumpster, or locker. While the player is inside,
    // guard vision detection is multiplied by visibilityMultiplier (default 0 = fully hidden).
    // Guards still react to noise, so crouching inside is recommended.
    [RequireComponent(typeof(Collider))]
    public sealed class HidingSpot : MonoBehaviour
    {
        [SerializeField] [Range(0f, 1f)] float visibilityMultiplier = 0f;

        static int playerOccupancy;
        static float activeMultiplier = 1f;

        public static float CurrentVisibility => playerOccupancy > 0 ? activeMultiplier : 1f;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameplayTags.Player)) return;
            playerOccupancy++;
            activeMultiplier = visibilityMultiplier;
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(GameplayTags.Player)) return;
            playerOccupancy = Mathf.Max(0, playerOccupancy - 1);
            if (playerOccupancy == 0) activeMultiplier = 1f;
        }

        void OnDisable()
        {
            playerOccupancy = 0;
            activeMultiplier = 1f;
        }
    }
}
