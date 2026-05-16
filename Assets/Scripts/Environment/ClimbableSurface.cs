using NutHeist.Core;
using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>Marks geometry that should advertise the climb gameplay tag automatically.</summary>
    [ExecuteAlways]
    public sealed class ClimbableSurface : MonoBehaviour
    {
        [SerializeField] bool enforceTagAtRuntime = true;

        void OnEnable()
        {
            ApplyTagIfNeeded();
        }

        void ApplyTagIfNeeded()
        {
            if (!enforceTagAtRuntime)
            {
                return;
            }

            try
            {
                if (!gameObject.CompareTag(GameplayTags.Climbable))
                {
                    gameObject.tag = GameplayTags.Climbable;
                }
            }
            catch
            {
                // Tag absent until Bootstrap runs.
            }
        }
    }
}
