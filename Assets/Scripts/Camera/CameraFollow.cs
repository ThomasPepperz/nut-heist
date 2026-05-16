using Cinemachine;
using NutHeist.Player;
using UnityEngine;

namespace NutHeist.CameraRig
{
    /// <summary>Minimal Cinemachine profile swap for exploration vs climbing context.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] SquirrelController squirrel;
        [SerializeField] Transform cameraTarget;
        [SerializeField] CinemachineVirtualCamera roamCamera;
        [SerializeField] CinemachineVirtualCamera climbCamera;

        ClimbingSystem _climbBrain;

        void Awake()
        {
            squirrel ??= FindFirstObjectByType<SquirrelController>();
            if (squirrel)
            {
                _climbBrain = squirrel.GetComponent<ClimbingSystem>();
            }

            if (roamCamera && cameraTarget)
            {
                roamCamera.Follow = cameraTarget;
                roamCamera.LookAt = cameraTarget;
                roamCamera.m_Lens.FieldOfView = 65f;
                roamCamera.m_Lens.NearClipPlane = 0.1f;
                roamCamera.m_Lens.FarClipPlane = 500f;
                EnsureCollider(roamCamera);
            }

            if (climbCamera && cameraTarget)
            {
                climbCamera.Follow = cameraTarget;
                climbCamera.LookAt = cameraTarget;
                climbCamera.m_Lens.FieldOfView = 70f;
                climbCamera.m_Lens.NearClipPlane = 0.1f;
                climbCamera.m_Lens.FarClipPlane = 500f;
                EnsureCollider(climbCamera);
            }

            ApplyClimbingState(false);
        }

        static void EnsureCollider(CinemachineVirtualCamera cam)
        {
            if (!cam.gameObject.TryGetComponent(out CinemachineCollider _))
            {
                cam.gameObject.AddComponent<CinemachineCollider>();
            }
        }

        void OnEnable()
        {
            if (_climbBrain)
            {
                _climbBrain.ClimbingChanged += ApplyClimbingState;
            }
        }

        void OnDisable()
        {
            if (_climbBrain)
            {
                _climbBrain.ClimbingChanged -= ApplyClimbingState;
            }
        }

        void ApplyClimbingState(bool climbing)
        {
            if (!roamCamera || !climbCamera)
            {
                return;
            }

            roamCamera.Priority = climbing ? 10 : 20;
            climbCamera.Priority = climbing ? 26 : 5;
            climbCamera.gameObject.SetActive(true);
            roamCamera.gameObject.SetActive(true);
        }
    }
}
