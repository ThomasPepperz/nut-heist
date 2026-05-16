using NutHeist.Pickups;
using UnityEngine;

namespace NutHeist.World
{
    /// <summary>Authoring helper for scattering placeholder nuts inside a yard Bounds.</summary>
    public sealed class WorldSpawner : MonoBehaviour
    {
        [SerializeField]
        NutPickup nutPrefabPrototype;

        [SerializeField]
        Bounds yardBounds = new Bounds(Vector3.zero, new Vector3(120f, 5f, 120f));

        [SerializeField]
        int nutsToScatter = 40;

        void Start()
        {
            ScatterNutsIfPossible();
        }

        [ContextMenu("Scatter nuts now")]
        public void ScatterNutsIfPossible()
        {
            if (!nutPrefabPrototype)
            {
                return;
            }

            for (int i = 0; i < nutsToScatter; ++i)
            {
                Vector3 jitter = RandomInsideBounds(yardBounds);
                Quaternion facing = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Instantiate(nutPrefabPrototype, jitter + Vector3.up * 0.1f, facing, transform);
            }
        }

        static Vector3 RandomInsideBounds(Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Mathf.Max(Random.Range(bounds.min.y, bounds.max.y), 0.05f),
                Random.Range(bounds.min.z, bounds.max.z));
        }
    }
}
