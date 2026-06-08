using UnityEngine;

namespace NutHeist.AI
{
    // Placed on the squirrel. Outputs a noise radius that GuardPerception polls each frame.
    // Sprinting → large radius. Walking → medium. Crouching → tiny. Stationary → 0.
    // Landing emits a one-shot burst that decays quickly.
    [RequireComponent(typeof(NutHeist.Player.SquirrelController))]
    public sealed class NoiseEmitter : MonoBehaviour
    {
        [SerializeField] float walkRadius   = 3.5f;
        [SerializeField] float sprintRadius = 8f;
        [SerializeField] float crouchRadius = 0.8f;
        [SerializeField] float landingRadius = 12f;
        [SerializeField] float landingDecayRate = 6f;

        NutHeist.Player.SquirrelController squirrel;
        float burstRadius;

        public float CurrentRadius => Mathf.Max(steadyRadius, burstRadius);
        float steadyRadius;

        void Awake() => squirrel = GetComponent<NutHeist.Player.SquirrelController>();

        void Update()
        {
            burstRadius = Mathf.Max(0f, burstRadius - Time.deltaTime * landingDecayRate);

            bool moving = squirrel.HorizontalVelocity.sqrMagnitude > 0.4f;
            bool crouching = squirrel.InputReader.CrouchHeld;
            bool sprinting = squirrel.InputReader.SprintHeld;

            switch (squirrel.CurrentState)
            {
                case NutHeist.Player.MovementState.Walking:
                    if (!moving) { steadyRadius = 0f; break; }
                    steadyRadius = crouching ? crouchRadius : sprinting ? sprintRadius : walkRadius;
                    break;
                case NutHeist.Player.MovementState.Swimming:
                    steadyRadius = walkRadius;
                    break;
                case NutHeist.Player.MovementState.Climbing:
                    steadyRadius = crouchRadius;
                    break;
                default:
                    steadyRadius = 0f;
                    break;
            }
        }

        // Call when the squirrel lands hard (from SquirrelController.OnControllerColliderHit).
        public void EmitLandingBurst() => burstRadius = landingRadius;

        // Call to emit an arbitrary noise at this position (e.g. thrown prop landing).
        public static void EmitAt(Vector3 worldPos, float radius)
        {
            GuardAlertNetwork.Instance?.BroadcastNoise(worldPos, radius);
        }
    }
}
