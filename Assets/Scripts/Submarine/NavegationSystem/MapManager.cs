using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    private Canvas _mapCanvas;
    private Submarine2DMovementBehaviour _mapSubmarine;
    private WaypointManager _waypointManager;
    private List<MapIcon> _mapIcons;
    public Canvas MapCanvas => _mapCanvas;

    private void Awake()
    {
        _mapCanvas = GetComponent<Canvas>();
        _waypointManager = GetComponent<WaypointManager>();
        _mapSubmarine = GetComponentInChildren<Submarine2DMovementBehaviour>();
        _waypointManager.SubmarineRect = _mapSubmarine.GetComponent<RectTransform>();
        
    }
    
    private void OnEnable()
    {
        _mapSubmarine.OnWaypointReached += OnReachRemoveWaypoint;
        _waypointManager.OnRouteStarted += OnStartTravelingSubmarine;
        _waypointManager.OnRouteModified += OnUpdateSubmarineRoute;
    }
    
    private void OnDisable()
    {
        _mapSubmarine.OnWaypointReached -= OnReachRemoveWaypoint;
        _waypointManager.OnRouteStarted -= OnStartTravelingSubmarine;
        _waypointManager.OnRouteModified -= OnUpdateSubmarineRoute;
    }

    private void OnStartTravelingSubmarine()
    {
        OnUpdateSubmarineRoute();
        _mapSubmarine.UpdateToNewWaypointList();
    }
    
    private void OnReachRemoveWaypoint()
    {
        _waypointManager.RemoveWaypointOnArrival();
        OnUpdateSubmarineRoute();
    }
    
    private void OnUpdateSubmarineRoute()
    {
        var waypoints = _waypointManager.GetWaypoints();
        _mapSubmarine.GetNewWaypointList(waypoints.Select(icon => icon.GetComponent<RectTransform>()).ToList());
    }
}
