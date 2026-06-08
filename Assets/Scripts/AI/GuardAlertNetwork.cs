using System.Collections.Generic;
using UnityEngine;

namespace NutHeist.AI
{
    // Singleton service. Guards register on Awake, deregister on destroy.
    // When any guard goes Alerted it calls Broadcast() to wake nearby guards.
    // Thrown props call BroadcastNoise() to alert guards within a radius.
    public sealed class GuardAlertNetwork : MonoBehaviour
    {
        public static GuardAlertNetwork Instance { get; private set; }

        [SerializeField] float alertBroadcastRadius = 22f;

        readonly List<GuardBrain> guards = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Register(GuardBrain g)   { if (!guards.Contains(g)) guards.Add(g); }
        public void Deregister(GuardBrain g) { guards.Remove(g); }

        // Call when a guard goes Alerted — wakes other guards within alertBroadcastRadius.
        public void Broadcast(GuardBrain source, Vector3 playerWorldPos)
        {
            foreach (GuardBrain g in guards)
            {
                if (g == null || g == source) continue;
                if (Vector3.Distance(g.transform.position, source.transform.position) <= alertBroadcastRadius)
                    g.ForceAlert(playerWorldPos);
            }
        }

        // Call when a noise event occurs (landing, thrown prop collision).
        // Any guard whose hearing origin is within radius of worldPos gets a ForceAlert.
        public void BroadcastNoise(Vector3 worldPos, float radius)
        {
            foreach (GuardBrain g in guards)
            {
                if (g == null) continue;
                if (Vector3.Distance(g.transform.position, worldPos) <= radius)
                    g.ForceAlert(worldPos);
            }
        }
    }
}
