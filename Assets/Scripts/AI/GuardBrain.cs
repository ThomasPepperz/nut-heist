using NutHeist.Core;
using UnityEngine;
using UnityEngine.AI;

namespace NutHeist.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(GuardPerception))]
    public sealed class GuardBrain : MonoBehaviour
    {
        [Header("Patrol")]
        [SerializeField] PatrolRoute route;
        [SerializeField] float patrolSpeed = 3f;
        [SerializeField] float patrolWaitTime = 1.5f;

        [Header("Detection Thresholds")]
        [SerializeField] float suspiciousThreshold = 0.30f;
        [SerializeField] float alertedThreshold = 0.70f;

        [Header("Chase / Search")]
        [SerializeField] float chaseSpeed = 7f;
        [SerializeField] float catchRadius = 1.2f;
        [SerializeField] float searchDuration = 8f;

        NavMeshAgent agent;
        GuardPerception perception;

        AlertLevel alertLevel;
        int waypointIndex;
        float waitTimer;
        float suspicionTimer;
        Vector3 lastKnownPlayerPos;

        public AlertLevel CurrentAlert => alertLevel;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<GuardPerception>();
            GuardAlertNetwork.Instance?.Register(this);
        }

        void OnDestroy() => GuardAlertNetwork.Instance?.Deregister(this);

        void OnEnable()  => GuardAlertNetwork.Instance?.Register(this);
        void OnDisable() => GuardAlertNetwork.Instance?.Deregister(this);

        void Update()
        {
            float detection = perception.Evaluate();

            switch (alertLevel)
            {
                case AlertLevel.None:       TickPatrol(detection);     break;
                case AlertLevel.Suspicious: TickSuspicious(detection); break;
                case AlertLevel.Alerted:    TickChase(detection);      break;
            }
        }

        void TickPatrol(float detection)
        {
            agent.speed = patrolSpeed;

            if (detection >= suspiciousThreshold)
            {
                alertLevel = AlertLevel.Suspicious;
                suspicionTimer = 2.5f;
                agent.ResetPath();
                return;
            }

            if (route == null || route.Count == 0) return;

            if (!agent.hasPath || agent.remainingDistance < 0.4f)
            {
                if (waitTimer > 0f) { waitTimer -= Time.deltaTime; return; }
                agent.SetDestination(route.GetWaypoint(waypointIndex));
                waypointIndex = (waypointIndex + 1) % Mathf.Max(route.Count, 1);
                waitTimer = patrolWaitTime;
            }
        }

        void TickSuspicious(float detection)
        {
            suspicionTimer -= Time.deltaTime;

            if (detection >= alertedThreshold)
            {
                alertLevel = AlertLevel.Alerted;
                agent.speed = chaseSpeed;
                RecordLastKnown();
                GuardAlertNetwork.Instance?.Broadcast(this, lastKnownPlayerPos);
                return;
            }

            if (detection < 0.05f && suspicionTimer <= 0f)
            {
                alertLevel = AlertLevel.None;
                return;
            }

            // Stand still while investigating.
            agent.ResetPath();
        }

        void TickChase(float detection)
        {
            if (detection > 0f)
            {
                RecordLastKnown();
                agent.SetDestination(lastKnownPlayerPos);

                if (perception.PlayerTransform &&
                    Vector3.Distance(transform.position, perception.PlayerTransform.position) <= catchRadius)
                {
                    GameLoop.Instance?.NotifyCaught();
                }
            }
            else
            {
                // Lost sight — search last known position, then give up.
                agent.SetDestination(lastKnownPlayerPos);
                if (agent.remainingDistance < 0.5f)
                {
                    alertLevel = AlertLevel.Suspicious;
                    suspicionTimer = searchDuration;
                }
            }
        }

        void RecordLastKnown()
        {
            if (perception.PlayerTransform)
                lastKnownPlayerPos = perception.PlayerTransform.position;
        }

        // External trigger (noise event, scripted alert).
        public void ForceAlert(Vector3 noiseWorldPos)
        {
            alertLevel = AlertLevel.Alerted;
            agent.speed = chaseSpeed;
            lastKnownPlayerPos = noiseWorldPos;
            agent.SetDestination(noiseWorldPos);
        }
    }
}
