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
        _mapSubmarine.OnWaypointReached += OnRemoveWaypoint;
        _waypointManager.OnRouteStarted += UpdateSubmarineRoute;
        _waypointManager.OnRouteModified += UpdateSubmarineRoute;
        _waypointManager.OnRouteCancelled += _mapSubmarine.StopMovingTowards;
    }
    
    private void OnDisable()
    {
        _mapSubmarine.OnWaypointReached -= OnRemoveWaypoint;
        _waypointManager.OnRouteStarted -= UpdateSubmarineRoute;
        _waypointManager.OnRouteModified -= UpdateSubmarineRoute;
        _waypointManager.OnRouteCancelled -= _mapSubmarine.StopMovingTowards;
    }

    private void OnRemoveWaypoint()
    {
        _waypointManager.RemoveWaypointOnArrival();
        UpdateSubmarineRoute();
    }
    
    private void UpdateSubmarineRoute()
    {
        var waypoints = _waypointManager.GetWaypoints();
        _mapSubmarine.OnUpdateWaypointsList(waypoints.Select(icon => icon.GetComponent<RectTransform>()).ToList());
    }
}
