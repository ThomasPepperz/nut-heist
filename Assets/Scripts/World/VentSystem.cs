using UnityEngine;

namespace NutHeist.World
{
    /// <summary>Holds authoring constants for pneumatic crawlspaces (Section 9).</summary>
    public sealed class VentSystem : MonoBehaviour
    {
        public static VentSystem Instance { get; private set; }

        [Header("Vent Gameplay Constants")]
        public float ventDiameter = 0.35f;
        public float ventMoveSpeed = 5f;
        public bool ventCrouchAuto = true;
        public float airflowForce = 4f;
        public float condensationSlide = 8f;
        public float spiderWebSlow = 1.5f;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
