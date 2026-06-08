using TMPro;
using UnityEngine;

namespace NutHeist.AI
{
    // Attach to a guard and point the label field at a world-space TMP text above their head.
    // The text is billboard-rotated toward the main camera each frame.
    [RequireComponent(typeof(GuardBrain))]
    public sealed class AlertIndicator : MonoBehaviour
    {
        [SerializeField] TextMeshPro label;
        [SerializeField] Vector3 offset = new Vector3(0f, 2.4f, 0f);

        GuardBrain brain;
        Camera mainCam;

        void Awake()
        {
            brain = GetComponent<GuardBrain>();
            mainCam = Camera.main;
        }

        void LateUpdate()
        {
            if (!label) return;

            if (!mainCam || Camera.main != mainCam) mainCam = Camera.main;

            label.transform.position = transform.position + offset;

            if (mainCam)
                label.transform.forward = mainCam.transform.forward;

            switch (brain.CurrentAlert)
            {
                case AlertLevel.None:
                    label.text = string.Empty;
                    label.color = Color.white;
                    break;
                case AlertLevel.Suspicious:
                    label.text = "?";
                    label.color = Color.yellow;
                    break;
                case AlertLevel.Alerted:
                    label.text = "!";
                    label.color = Color.red;
                    break;
            }
        }
    }
}
