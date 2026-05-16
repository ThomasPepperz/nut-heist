using NutHeist.Audio;
using NutHeist.Environment;
using NutHeist.Core;
using UnityEngine;

namespace NutHeist.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SquirrelInput))]
    [RequireComponent(typeof(ClimbingSystem))]
    [DefaultExecutionOrder(0)]
    public sealed class SquirrelController : MonoBehaviour
    {
        SquirrelInput squirrelInput;
        ClimbingSystem climbing;
        CharacterController characterController;

        [Header("Movement")]
        public float moveSpeed = 8f;
        public float sprintSpeed = 10f;
        public float groundDrag = 6f;
        public float airDrag = 0.5f;
        public float acceleration = 15f;
        public float deceleration = 20f;

        [Header("Jumping")]
        public float jumpForce = 10f;
        public float doubleJumpForce = 8.5f;
        public float jumpHoldForce = 3f;
        public float maxJumpHoldTime = 0.25f;
        public float coyoteTime = 0.15f;
        public float jumpBufferTime = 0.10f;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;
        public float gravity = -20f;

        [Header("Ground Detection")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.1f;
        public LayerMask groundLayer = ~0;

        [Header("Swim / Vent")]
        [SerializeField] float swimForwardSpeed = 3f;
        [SerializeField] float ventForwardSpeed = 5f;

        [Header("References")]
        [SerializeField] SquirrelAnimator squirrelAnimatorOptional;

        Vector3 planarVelocity;
        float verticalVelocity;

        float coyoteTimer;
        float jumpBufferTimer;
        bool airJumpReady = true;

        bool grounded;
        bool jumpHoldAscend;

        float jumpHoldElapsed;
        int swimOverlaps;
        int ventOverlaps;
        float stunUntil;

        MovingPlatform riddenPlatform;
        Camera mainCamera;

        public CharacterController Cc => characterController;
        public bool IsGrounded => grounded;
        public SquirrelInput InputReader => squirrelInput;
        public bool InVentZone => ventOverlaps > 0;
        public bool InWater => swimOverlaps > 0;
        public Vector3 HorizontalVelocity => planarVelocity;
        public float VerticalSpeed => verticalVelocity;

        /// <summary>Airflow impulses applied this physics step.</summary>
        public Vector3 ExternalImpulse;

        /// <summary>Traversal zones may scale vent movement (slides, webs).</summary>
        public float VentTraversalScaler { get; set; } = 1f;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            squirrelInput = GetComponent<SquirrelInput>();
            climbing = GetComponent<ClimbingSystem>();
            climbing.AssignMotor(this);

            characterController.skinWidth = 0.01f;
            mainCamera = Camera.main;
            try
            {
                gameObject.tag = GameplayTags.Player;
            }
            catch (UnityException)
            {
                // Tags are created by Nut Heist → Full Project Setup before play.
            }
            SoundManager.Resolve();
        }

        void Start()
        {
            if (!groundCheck)
            {
                var gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform, false);
                gc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                groundCheck = gc.transform;
            }
        }

        public void NotifyWaterEnter() => swimOverlaps++;
        public void NotifyWaterExit() => swimOverlaps = Mathf.Max(0, swimOverlaps - 1);
        public void NotifyVentEnter() => ventOverlaps++;
        public void NotifyVentExit() => ventOverlaps = Mathf.Max(0, ventOverlaps - 1);

        public void LaunchVelocity(Vector3 worldVelocity)
        {
            planarVelocity = Vector3.Scale(worldVelocity, new Vector3(1f, 0f, 1f));
            verticalVelocity = worldVelocity.y;
            jumpHoldAscend = verticalVelocity > 0.08f;
            jumpHoldElapsed = 0f;
            airJumpReady = verticalVelocity <= 0.2f ? airJumpReady : true;
        }

        public void AddVerticalBurst(float impulseY)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, impulseY);
            jumpHoldAscend = true;
            jumpHoldElapsed = 0f;
        }

        public void ApplyKnockback(Vector3 horizontalKnockback, float stunSeconds)
        {
            planarVelocity = new Vector3(horizontalKnockback.x, 0f, horizontalKnockback.z);
            verticalVelocity = horizontalKnockback.y > Mathf.Epsilon ? horizontalKnockback.y : Mathf.Max(verticalVelocity, 4f);
            stunUntil = Time.time + Mathf.Max(stunSeconds, 0f);
        }

        void Update()
        {
            if (!mainCamera || Camera.main != mainCamera)
            {
                mainCamera = Camera.main;
            }

            RotateModelTowards(planarWishDir());
            squirrelAnimatorOptional?.Pump(this, climbing.IsActive);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            EvaluateGround(dt);

            if (squirrelInput.JumpPressedThisFrame)
            {
                jumpBufferTimer = jumpBufferTime;
            }

            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

            climbing.TickClimb(dt);

            if (Time.time < stunUntil)
            {
                verticalVelocity += gravity * fallMultiplier * 0.5f * dt;
                characterController.Move(planarVelocity * dt + Vector3.up * verticalVelocity * dt);
                squirrelAnimatorOptional?.Pump(this, climbing.IsActive);
                return;
            }

            if (climbing.IsActive)
            {
                planarVelocity = Vector3.zero;
                verticalVelocity = 0f;
                squirrelAnimatorOptional?.Pump(this, true);
                return;
            }

            TryResolveJumpRequests();

            Vector3 preamble = ExternalImpulse;
            ExternalImpulse = Vector3.zero;

            MovingPlatform plat = riddenPlatform;
            Vector3 platDelta = plat ? plat.LastFrameDelta : Vector3.zero;

            if (swimOverlaps > 0)
            {
                SwimMove(dt);
            }
            else if (ventOverlaps > 0)
            {
                VentMove(dt);
            }
            else
            {
                PlanarWalking(dt);
            }

            TickGravity(dt);
            TickVariableJumpAssist(dt);

            Vector3 planarStep = planarVelocity * dt + platDelta + preamble;
            Vector3 verticalStep = Vector3.up * verticalVelocity * dt;
            characterController.Move(planarStep + verticalStep);

            squirrelAnimatorOptional?.Pump(this, false);
        }

        void EvaluateGround(float dt)
        {
            grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
            coyoteTimer = grounded ? coyoteTime : coyoteTimer - dt;
            if (grounded && verticalVelocity < 0.25f && !jumpHoldAscend)
            {
                verticalVelocity = -2f;
                airJumpReady = true;
            }
        }

        void TryResolveJumpRequests()
        {
            if (jumpBufferTimer <= 0f)
            {
                return;
            }

            bool coyoteEligible = coyoteTimer > 0f || grounded;
            if (!InWater && ventOverlaps == 0 && coyoteEligible)
            {
                verticalVelocity = jumpForce;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                jumpHoldAscend = true;
                jumpHoldElapsed = 0f;
                return;
            }

            if (!InWater && ventOverlaps == 0 && airJumpReady && !coyoteEligible)
            {
                verticalVelocity = doubleJumpForce;
                jumpBufferTimer = 0f;
                airJumpReady = false;
                jumpHoldAscend = true;
                jumpHoldElapsed = 0f;
            }
        }

        void PlanarWalking(float dt)
        {
            Vector3 wishSpeed = planarWishDir() * (squirrelInput.SprintHeld ? sprintSpeed : moveSpeed);

            planarVelocity.y = 0f;

            float rate = wishSpeed.sqrMagnitude > 0.001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, wishSpeed, rate * dt);

            if (wishSpeed.sqrMagnitude < 0.001f)
            {
                float drag = Mathf.Exp(-((grounded ? groundDrag : airDrag) * dt));
                planarVelocity *= drag;
            }
        }

        void SwimMove(float dt)
        {
            coyoteTimer = 0f;
            planarVelocity = planarWishDir() * swimForwardSpeed;
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0f, Mathf.Abs(gravity) * dt);
        }

        void VentMove(float dt)
        {
            coyoteTimer = 0f;
            planarVelocity = planarWishDir() * ventForwardSpeed * VentTraversalScaler;
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0f, Mathf.Abs(gravity) * dt * 2f);
        }

        void TickGravity(float dt)
        {
            if (grounded && verticalVelocity < 0.5f && !jumpHoldAscend)
            {
                return;
            }

            float gv = gravity;
            if (!grounded && verticalVelocity < -0.01f)
            {
                gv *= fallMultiplier;
            }
            else if (verticalVelocity > 0.01f && !squirrelInput.JumpHeld)
            {
                gv *= lowJumpMultiplier;
            }

            verticalVelocity += gv * dt;
        }

        void TickVariableJumpAssist(float dt)
        {
            if (!jumpHoldAscend || verticalVelocity <= 0f || !squirrelInput.JumpHeld)
            {
                if (verticalVelocity <= 0f || !squirrelInput.JumpHeld)
                {
                    jumpHoldAscend = false;
                }
                return;
            }

            if (jumpHoldElapsed < maxJumpHoldTime)
            {
                float window = Mathf.Min(dt, maxJumpHoldTime - jumpHoldElapsed);
                verticalVelocity += jumpHoldForce * window;
                jumpHoldElapsed += window;
                if (jumpHoldElapsed >= maxJumpHoldTime)
                {
                    jumpHoldAscend = false;
                }
            }
            else
            {
                jumpHoldAscend = false;
            }
        }

        Vector3 planarWishDir()
        {
            Camera cam = mainCamera ? mainCamera : Camera.main;
            if (!cam)
            {
                Vector3 raw = new Vector3(squirrelInput.Move.x, 0f, squirrelInput.Move.y);
                return raw.sqrMagnitude > 1f ? raw.normalized : raw;
            }

            Vector3 fwd = cam.transform.forward;
            Vector3 right = cam.transform.right;
            fwd.y = right.y = 0f;
            fwd.Normalize();
            right.Normalize();
            Vector3 dir = fwd * squirrelInput.Move.y + right * squirrelInput.Move.x;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        void RotateModelTowards(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, Time.deltaTime * 540f);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.55f)
            {
                riddenPlatform = hit.collider.GetComponentInParent<MovingPlatform>();
            }
            else if (hit.normal.y < 0.2f)
            {
                if (riddenPlatform && hit.collider.GetComponentInParent<MovingPlatform>() != riddenPlatform)
                {
                    // intentionally keep riding last stable ground contact
                }
            }

            if (hit.normal.y > 0.65f && verticalVelocity <= -11f && SoundManager.Resolve())
            {
                SoundManager.Resolve().PlayLandingHeavy();
            }
        }

        void OnDisable()
        {
            riddenPlatform = null;
            swimOverlaps = 0;
            ventOverlaps = 0;
        }
    }
}
