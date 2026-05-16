using NutHeist.Core;
using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>Simple torque-less door hinge stand-in activated by squirrel proximity.</summary>
    public sealed class Openable : Interactable
    {
        [SerializeField]
        float openDegreesPerSecond = 35f;

        [SerializeField]
        float maximumYaw = 70f;

        float _progress;

        void OnTriggerStay(Collider colliderDetected)
        {
            if (!HasTier(InteractionTier.Openable))
            {
                return;
            }

            if (!colliderDetected.CompareTag(GameplayTags.Player))
            {
                return;
            }

            _progress += openDegreesPerSecond * Time.deltaTime;
            Quaternion target = Quaternion.Euler(0f, Mathf.Clamp(_progress, 0f, maximumYaw), 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 12f);
        }
    }
}
