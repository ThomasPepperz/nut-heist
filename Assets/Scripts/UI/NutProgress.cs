using System;
using UnityEngine;

namespace NutHeist.Progress
{
    /// <summary>Session-only collectible counter.</summary>
    public sealed class NutProgress : MonoBehaviour
    {
        public static NutProgress Instance { get; private set; }

        public event Action<int> TotalChanged;

        [SerializeField] int totalCollected;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int TotalCollected => totalCollected;

        public void CollectNut()
        {
            totalCollected++;
            TotalChanged?.Invoke(totalCollected);
        }
    }
}
