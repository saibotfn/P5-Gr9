
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaypointRouteFollower : MonoBehaviour
{
    [System.Serializable]
    public class RoutePoint
    {
        public Transform point;

        [Min(0.01f)]
        public float speedToThisPoint = 3f;

        [Min(0f)]
        public float waitTime = 0f;

        public UnityEvent onArrival;
    }

    [Header("Route")]
    public List<RoutePoint> waypoints = new List<RoutePoint>();

    [Header("Movement")]
    [Tooltip("Accelerate and decelerate smoothly on each segment.")]
    public bool smoothMovement = true;

    [Tooltip("Start the route automatically.")]
    public bool playOnStart = false;

    [Header("Events")]
    public UnityEvent onRouteStarted;
    public UnityEvent onRouteCompleted;

    private Coroutine routeCoroutine;

    public bool IsMoving => routeCoroutine != null;

    private void Start()
    {
        if (playOnStart)
            BeginRoute();
    }

    public void BeginRoute()
    {
        if (routeCoroutine != null)
            return;

        if (waypoints.Count == 0)
        {
            Debug.LogWarning(name + ": No waypoints assigned!");
            return;
        }

        routeCoroutine = StartCoroutine(FollowRoute());
    }

    public void StopRoute()
    {
        if (routeCoroutine == null)
            return;

        StopCoroutine(routeCoroutine);
        routeCoroutine = null;
    }

    public void RestartRoute()
    {
        StopRoute();
        BeginRoute();
    }

    private IEnumerator FollowRoute()
    {
        onRouteStarted.Invoke();

        foreach (RoutePoint waypoint in waypoints)
        {
            if (waypoint.point == null)
                continue;

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = waypoint.point.position;

            float distance = Vector3.Distance(
                startPosition,
                targetPosition
            );

            float speed = Mathf.Max(
                0.01f,
                waypoint.speedToThisPoint
            );

            float duration = distance / speed;

            if (distance > 0.001f)
            {
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;

                    float t = Mathf.Clamp01(elapsed / duration);

                    if (smoothMovement)
                        t = Mathf.SmoothStep(0f, 1f, t);

                    transform.position = Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t
                    );

                    yield return null;
                }
            }

            transform.position = targetPosition;

            waypoint.onArrival?.Invoke();

            if (waypoint.waitTime > 0f)
                yield return new WaitForSeconds(waypoint.waitTime);
        }

        routeCoroutine = null;
        onRouteCompleted.Invoke();
    }

    private void OnDisable()
    {
        StopRoute();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 previous = transform.position;

        foreach (RoutePoint waypoint in waypoints)
        {
            if (waypoint.point == null)
                continue;

            Gizmos.DrawLine(
                previous,
                waypoint.point.position
            );

            Gizmos.DrawWireSphere(
                waypoint.point.position,
                0.35f
            );

            previous = waypoint.point.position;
        }
    }
}
