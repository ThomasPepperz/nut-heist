using System;
using NutHeist.Core;
using UnityEngine;

namespace NutHeist.Player
{
    /// <summary>Wall climb / probe IK hooks. Drives CharacterController while active.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ClimbingSystem : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] float forwardCastDistance = 0.55f;
        [SerializeField] float sphereRadius = 0.12f;
        [SerializeField] Vector3 castOriginOffset = new Vector3(0f, 0.09f, 0.06f);
        [SerializeField] LayerMask obstructionMask = ~0;

        [Header("Motor")]
        [SerializeField] float climbSpeedUp = 5f;
        [SerializeField] float climbSpeedDown = 4f;
        [SerializeField] float climbSpeedLateral = 4.5f;
        [SerializeField] float wallJumpHoriz = 9.5f;
        [SerializeField] float wallJumpVert = 5.5f;

        [Header("Stamina (optional)")]
        [SerializeField] bool useStamina;
        [SerializeField] float maxClimbSeconds = 8f;

        [Header("Optional IK targets (Animation Rigging weights hook up here)")]
        [SerializeField] Transform leftHandIk;
        [SerializeField] Transform rightFootIk;
        [SerializeField] Transform rightHandIk;
        [SerializeField] Transform leftFootIk;

        SquirrelController motor;
        CharacterController body;

        bool climbing;
        Vector3 surfaceNormal;
        float staminaRemaining;
        float ikCooldown;

        public bool IsActive => climbing;
        public event Action<bool> ClimbingChanged;

        public void AssignMotor(SquirrelController squirrel)
        {
            motor = squirrel;
            body = squirrel != null ? squirrel.Cc : GetComponent<CharacterController>();
        }

        void Awake()
        {
            staminaRemaining = maxClimbSeconds;
            if (!body)
            {
                body = GetComponent<CharacterController>();
            }
        }

        public void TickClimb(float dt)
        {
            if (!motor || body == null)
            {
                return;
            }

            Vector3 origin = motor.transform.TransformPoint(castOriginOffset);
            Vector3 forward = motor.transform.forward;

            bool wallValid = Physics.SphereCast(
                origin,
                sphereRadius,
                forward,
                out RaycastHit wallHit,
                forwardCastDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore);

            wallValid = wallValid && wallHit.collider && wallHit.collider.CompareTag(GameplayTags.Climbable);

            if (!climbing)
            {
                if (wallValid)
                {
                    bool runIn = Vector3.Dot(motor.transform.forward, planarMotorWish()) > 0.45f;
                    if (motor.InputReader.JumpPressedThisFrame || runIn)
                    {
                        BeginClimb(wallHit.normal);
                    }
                }

                return;
            }

            if (!wallValid || (useStamina && staminaRemaining <= 0f))
            {
                EndClimb();
                return;
            }

            staminaRemaining -= dt;
            surfaceNormal = wallHit.normal;

            if (motor.InputReader.JumpPressedThisFrame)
            {
                WallJump();
                return;
            }

            Vector3 upAxis = Vector3.ProjectOnPlane(Vector3.up, surfaceNormal);
            if (upAxis.sqrMagnitude < 1e-4f)
            {
                upAxis = Vector3.ProjectOnPlane(motor.transform.up, surfaceNormal);
            }

            upAxis.Normalize();
            Vector3 rightAxis = Vector3.Cross(surfaceNormal, upAxis);
            if (rightAxis.sqrMagnitude < 1e-4f)
            {
                rightAxis = Vector3.Cross(surfaceNormal, Vector3.forward);
            }

            rightAxis.Normalize();

            float v = motor.InputReader.Move.y;
            float h = motor.InputReader.Move.x;
            Vector3 climbStep = Vector3.zero;
            if (Mathf.Abs(v) > 0.01f)
            {
                float vy = Mathf.Clamp01(Mathf.Abs(v));
                climbStep += upAxis * (v >= 0f ? climbSpeedUp : climbSpeedDown) * vy;
            }

            if (Mathf.Abs(h) > 0.01f)
            {
                float hx = Mathf.Clamp01(Mathf.Abs(h));
                climbStep += rightAxis * (h >= 0f ? climbSpeedLateral : -climbSpeedLateral) * hx;
            }

            body.Move(climbStep * dt);

            Quaternion targetFacing = Quaternion.LookRotation(-surfaceNormal, Vector3.up);
            motor.transform.rotation = Quaternion.Slerp(motor.transform.rotation, targetFacing, dt * 8f);

            StepIkPulse(wallHit);
        }

        Vector3 planarMotorWish()
        {
            Vector3 v = motor.transform.forward * motor.InputReader.Move.y + motor.transform.right * motor.InputReader.Move.x;
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.zero;
        }

        void BeginClimb(Vector3 normal)
        {
            climbing = true;
            surfaceNormal = normal;
            staminaRemaining = maxClimbSeconds;
            ikCooldown = 0f;
            ClimbingChanged?.Invoke(true);
        }

        void EndClimb()
        {
            if (!climbing)
            {
                return;
            }

            climbing = false;
            ClimbingChanged?.Invoke(false);
        }

        void WallJump()
        {
            Vector3 away = (-surfaceNormal + Vector3.up).normalized;
            motor.LaunchVelocity(new Vector3(away.x * wallJumpHoriz, Mathf.Max(wallJumpVert * away.y, wallJumpVert * 0.35f), away.z * wallJumpHoriz));
            EndClimb();
        }

        void StepIkPulse(RaycastHit wallHit)
        {
            ikCooldown -= Time.fixedDeltaTime;
            if (ikCooldown > 0f)
            {
                return;
            }

            ikCooldown = 0.08f;

            void Snap(Transform target, Vector3 local)
            {
                if (!target)
                {
                    return;
                }

                Vector3 rayStart = motor.transform.TransformPoint(local);
                if (Physics.Raycast(rayStart, -surfaceNormal, out RaycastHit limbHit, 0.65f))
                {
                    target.position = limbHit.point;
                    target.rotation = Quaternion.LookRotation(limbHit.normal);
                }
            }

            Snap(leftHandIk, new Vector3(-0.08f, 0.08f, 0.06f));
            Snap(rightHandIk, new Vector3(0.08f, 0.08f, 0.06f));
            Snap(leftFootIk, new Vector3(-0.05f, -0.02f, 0.06f));
            Snap(rightFootIk, new Vector3(0.05f, -0.02f, 0.06f));
        }
    }
}
