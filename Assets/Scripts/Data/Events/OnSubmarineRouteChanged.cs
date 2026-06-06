using System.Collections.Generic;
using UnityEngine;

public struct OnSubmarineRouteChanged : IGameEvent
{
    public List<Vector3> _waypoints;

    public OnSubmarineRouteChanged(List<Vector3> waypoints)
    {
        _waypoints = waypoints;
    }
}
