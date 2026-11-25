
using UnityEngine;
using StarterAssets;

public class PlayerFlightMode : MonoBehaviour
{
    [Header("Flight Settings")]
    [Tooltip("ON = automatic flight, OFF = WASD movement")]
    public bool automaticFlight = true;

    [Tooltip("Begin flying automatically when Play starts")]
    public bool startFlightOnPlay = true;

    [Header("References")]
    public WaypointRouteFollower routeFollower;
    [SerializeField] private StormManager stormManager;

    [Tooltip("First Person Controller on PlayerCapsule")]
    public FirstPersonController firstPersonController;

    private bool previousFlightMode;

    private void Awake()
    {
        if (routeFollower == null)
            routeFollower = GetComponent<WaypointRouteFollower>();

        if (routeFollower != null)
            routeFollower.playOnStart = false;

        if (firstPersonController == null)
        {
            firstPersonController =
                GetComponentInChildren<FirstPersonController>();
        }

        previousFlightMode = automaticFlight;
        ApplyFlightMode();
    }

    private void Start()
    {
        if (automaticFlight && startFlightOnPlay)
            StartFlight();
    }

    private void Update()
    {
        if (previousFlightMode != automaticFlight)
        {
            previousFlightMode = automaticFlight;

            ApplyFlightMode();

            if (automaticFlight && startFlightOnPlay)
                StartFlight();
        }
    }

    private void ApplyFlightMode()
    {
        if (firstPersonController != null)
        {
            // Keep the controller enabled so mouse look works.
            firstPersonController.enabled = true;

            // Disable only WASD, jumping and gravity.
            firstPersonController.SetMovementAllowed(
                !automaticFlight
            );
        }

        if (!automaticFlight && routeFollower != null)
            routeFollower.StopRoute();
    }

    public void StartFlight()
    {
        if (!automaticFlight || routeFollower == null)
            return;

        routeFollower.BeginRoute();
    }

    public void StopFlight()
    {
        if (routeFollower != null)
            routeFollower.StopRoute();
        //if(stormManager != null)
            //stormManager.BeginStorm();
    }

    public void SetAutomaticFlight(bool enabled)
    {
        automaticFlight = enabled;
        previousFlightMode = enabled;

        ApplyFlightMode();
    }
}
