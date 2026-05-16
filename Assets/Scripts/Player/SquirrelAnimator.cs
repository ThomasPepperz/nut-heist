using UnityEngine;

namespace NutHeist.Player
{
    /// <summary>Feeds Animator whenever an Animator is wired on this object.</summary>
    public sealed class SquirrelAnimator : MonoBehaviour
    {
        [SerializeField] Animator animatorTarget;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int GroundId = Animator.StringToHash("Grounded");
        static readonly int ClimbId = Animator.StringToHash("Climbing");
        static readonly int VertId = Animator.StringToHash("VerticalSpeed");

        void Awake()
        {
            animatorTarget ??= GetComponentInChildren<Animator>();
        }

        public void Pump(SquirrelController body, bool climbing)
        {
            if (!animatorTarget)
            {
                return;
            }

            animatorTarget.SetFloat(SpeedId, body.HorizontalVelocity.magnitude);
            animatorTarget.SetBool(GroundId, body.IsGrounded);
            animatorTarget.SetBool(ClimbId, climbing);
            animatorTarget.SetFloat(VertId, climbing ? 0f : body.VerticalSpeed);
        }
    }
}
