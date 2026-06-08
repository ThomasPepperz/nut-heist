using NutHeist.Core;
using UnityEngine;

namespace NutHeist.UI
{
    public sealed class GameLoopUI : MonoBehaviour
    {
        public static GameLoopUI Instance { get; private set; }

        [SerializeField] GameObject wonPanel;
        [SerializeField] GameObject caughtPanel;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowWon()
        {
            wonPanel?.SetActive(true);
            caughtPanel?.SetActive(false);
        }

        public void ShowCaught()
        {
            caughtPanel?.SetActive(true);
            wonPanel?.SetActive(false);
        }

        public void HideAll()
        {
            wonPanel?.SetActive(false);
            caughtPanel?.SetActive(false);
        }

        // Wire to a Restart button's OnClick in the inspector.
        public void OnRestartButton() => GameLoop.Instance?.Restart();
    }
}
