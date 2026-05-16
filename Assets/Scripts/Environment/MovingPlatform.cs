using UnityEngine;

namespace NutHeist.Environment
{
    /// <summary>Waypoint follower that reports per-step motion for CharacterController grounding.</summary>
    [DefaultExecutionOrder(-120)]
    public sealed class MovingPlatform : MonoBehaviour
    {
        public Transform[] waypoints;
        public float speed = 2f;
        public Vector3 LastFrameDelta { get; private set; }

        int _cursor;
        int _direction = 1;

        void FixedUpdate()
        {
            Vector3 prev = transform.position;

            if (waypoints != null && waypoints.Length >= 2)
            {
                Transform target = waypoints[_cursor];
                transform.position =
                    Vector3.MoveTowards(transform.position, target.position, speed * Time.fixedDeltaTime);

                if ((transform.position - target.position).sqrMagnitude < 0.0004f)
                {
                    AdvanceCursor();
                }
            }

            LastFrameDelta = transform.position - prev;
        }

        void AdvanceCursor()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return;
            }

            int next = _cursor + _direction;
            if (next >= waypoints.Length || next < 0)
            {
                _direction *= -1;
                next = _cursor + _direction;
            }

            _cursor = Mathf.Clamp(next, 0, waypoints.Length - 1);
        }
    }
}
