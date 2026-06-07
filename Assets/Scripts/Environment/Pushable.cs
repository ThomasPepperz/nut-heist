using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>
    /// Marks a rigidbody as squirrel-pushable.
    /// The actual impulse is applied by SquirrelController.OnControllerColliderHit
    /// whenever the squirrel walks into this object — no button press required.
    /// Adjust pushResistance to make heavier objects harder to slide.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Pushable : Interactable
    {
        [SerializeField] float pushResistance = 1f;

        public float PushResistance => pushResistance;

        void Awake()
        {
            // Mirror the authoring mass onto the Rigidbody so physics weight
            // and the carry-weight check both use the same number.
            GetComponent<Rigidbody>().mass = MassKg;
        }

        // Pushing is passive — no prompt shown.
        public override string GetPrompt() => string.Empty;
    }
}
