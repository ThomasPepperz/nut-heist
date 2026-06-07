using NutHeist.Environment;
using UnityEngine;

namespace NutHeist.Player
{
    /// <summary>
    /// Manages picking up and physically holding a single Interactable prop.
    /// Attach alongside SquirrelController on the squirrel prefab.
    ///
    /// How it works: TryPickup() makes the rigidbody kinematic and disables
    /// colliders so the prop doesn't collide with the squirrel while held.
    /// LateUpdate lerps the prop to a carry anchor offset in local space.
    /// Drop() restores physics and fires the prop with zero velocity so it
    /// falls naturally from wherever the squirrel is standing.
    /// </summary>
    public sealed class CarrySystem : MonoBehaviour
    {
        // World-space offset relative to the squirrel where the held prop floats.
        [SerializeField] Vector3 carryOffset = new Vector3(0f, 0.18f, 0.22f);
        [SerializeField] float carryLerpSpeed = 18f;
        // Objects heavier than this cannot be picked up.
        [SerializeField] float maxCarryMassKg = 10f;

        public bool IsCarrying => heldItem != null;
        public Interactable HeldItem => heldItem;

        Interactable heldItem;
        Rigidbody heldRb;
        Collider[] heldColliders;

        /// <summary>Returns true if the item was successfully picked up.</summary>
        public bool TryPickup(Interactable item)
        {
            if (IsCarrying) return false;
            if (item == null) return false;
            if (item.MassKg > maxCarryMassKg) return false;

            heldItem = item;
            heldRb = item.GetComponent<Rigidbody>();
            heldColliders = item.GetComponentsInChildren<Collider>(includeInactive: true);

            if (heldRb)
            {
                heldRb.isKinematic = true;
            }

            // Disable colliders to prevent the prop fighting with the CharacterController.
            foreach (var col in heldColliders)
            {
                col.enabled = false;
            }

            return true;
        }

        public void Drop()
        {
            if (!IsCarrying) return;

            foreach (var col in heldColliders)
            {
                if (col) col.enabled = true;
            }

            if (heldRb)
            {
                heldRb.isKinematic = false;
                heldRb.linearVelocity = Vector3.zero;
            }

            heldItem = null;
            heldRb = null;
            heldColliders = null;
        }

        void LateUpdate()
        {
            if (!IsCarrying) return;

            Vector3 worldTarget = transform.TransformPoint(carryOffset);
            heldItem.transform.position = Vector3.Lerp(
                heldItem.transform.position,
                worldTarget,
                Time.deltaTime * carryLerpSpeed);

            heldItem.transform.rotation = Quaternion.Slerp(
                heldItem.transform.rotation,
                transform.rotation,
                Time.deltaTime * carryLerpSpeed);
        }
    }
}
