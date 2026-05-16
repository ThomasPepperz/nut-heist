using NutHeist.Progress;
using UnityEngine;
using UnityEngine.UI;

namespace NutHeist.UI
{
    public sealed class NutHudView : MonoBehaviour
    {
        [SerializeField] Text nutCounterText;
        [SerializeField] Text dayLabelText;

        void Start()
        {
            if (!nutCounterText)
            {
                return;
            }

            if (!NutProgress.Instance)
            {
                var host = new GameObject("NutProgress");
                host.AddComponent<NutProgress>();
            }
            if (NutProgress.Instance)
            {
                NutProgress.Instance.TotalChanged += OnTotalChanged;
                OnTotalChanged(NutProgress.Instance.TotalCollected);
            }

            if (dayLabelText)
            {
                dayLabelText.text = "Day 1";
            }
        }

        void OnDestroy()
        {
            if (NutProgress.Instance)
            {
                NutProgress.Instance.TotalChanged -= OnTotalChanged;
            }
        }

        void OnTotalChanged(int total)
        {
            if (!nutCounterText)
            {
                return;
            }

            nutCounterText.text = $"{total}";
        }
    }
}
