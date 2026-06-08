using UnityEngine;

namespace NutHeist.AI
{
    public sealed class GuardPerception : MonoBehaviour
    {
        [SerializeField] float visionRange = 14f;
        [SerializeField] [Range(5f, 90f)] float visionHalfAngle = 55f;
        [SerializeField] float hearingRadius = 4f;
        [SerializeField] LayerMask occlusionMask = ~0;

        Transform playerTransform;

        public Transform PlayerTransform => playerTransform;

        void Start()
        {
            GameObject playerObj = GameObject.FindWithTag(NutHeist.Core.GameplayTags.Player);
            if (playerObj) playerTransform = playerObj.transform;
        }

        // Returns 0 (undetected) → 1 (fully detected).
        public float Evaluate()
        {
            if (!playerTransform) return 0f;

            Vector3 toPlayer = playerTransform.position - EyePosition;
            float dist = toPlayer.magnitude;

            if (dist <= hearingRadius) return 1f;
            if (dist > visionRange) return 0f;

            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > visionHalfAngle) return 0f;

            if (Physics.Raycast(EyePosition, toPlayer / dist, dist, occlusionMask,
                    QueryTriggerInteraction.Ignore))
                return 0f;

            float distFactor = 1f - (dist / visionRange);
            float angleFactor = 1f - (angle / visionHalfAngle);
            return Mathf.Clamp01((distFactor + angleFactor) * 0.5f);
        }

        Vector3 EyePosition => transform.position + Vector3.up * 1.4f;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, hearingRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Vector3 eye = EyePosition;
            Vector3 fwdScaled = transform.forward * visionRange;
            Vector3 left = Quaternion.Euler(0f, -visionHalfAngle, 0f) * fwdScaled;
            Vector3 right = Quaternion.Euler(0f, visionHalfAngle, 0f) * fwdScaled;
            Gizmos.DrawRay(eye, fwdScaled);
            Gizmos.DrawRay(eye, left);
            Gizmos.DrawRay(eye, right);
        }
    }
}
