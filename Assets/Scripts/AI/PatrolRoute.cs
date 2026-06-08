using UnityEngine;

namespace NutHeist.AI
{
    public sealed class PatrolRoute : MonoBehaviour
    {
        [SerializeField] Transform[] waypoints;

        public int Count => waypoints?.Length ?? 0;

        public Vector3 GetWaypoint(int index)
        {
            if (waypoints == null || waypoints.Length == 0) return transform.position;
            return waypoints[index % waypoints.Length].position;
        }

        void OnDrawGizmosSelected()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (!waypoints[i]) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next])
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }
    }
}
