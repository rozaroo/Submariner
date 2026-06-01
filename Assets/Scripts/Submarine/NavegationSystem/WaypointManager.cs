using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class WaypointManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Properties")] 
    [SerializeField] private MapAssetSO waypointPointConfig;
    [SerializeField] private GameObject lineContainer;
    
    private readonly  List<MapIcon> _waypoints = new();
    private RectTransform _mapRect;
    private RectTransform _submarineRect;
    public RectTransform MapRect
    {
        set => _mapRect = value;
    }

    public RectTransform SubmarineRect
    {
        set => _submarineRect = value;
    }
    
    public event Action OnRouteStarted;
    public event Action OnRouteModified;

    #region Pointer Events Handlers

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            HandleLeftClick(eventData);
    }

    private void HandleLeftClick(PointerEventData eventData)
    {
        if (_submarineRect == null || _mapRect == null)
        {
            Log.Info($"[{name}]- No Map Rect or Submarine");
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _mapRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        
        var mapIcon = CreateMapIcon(localPoint);
        var iconLineBehaviour = mapIcon.gameObject.GetComponent<LineBehaviour>();
        
        SetWaypoint(mapIcon, iconLineBehaviour);
        RefreshIndices();
    }

    #endregion

    #region WaypointUtilities

    private MapIcon CreateMapIcon(Vector2 point)
    {
        MapIcon mapIcon = MapIconFactory.Create(waypointPointConfig, _mapRect);
        mapIcon.GetComponent<RectTransform>().anchoredPosition = point;
        return mapIcon;
    }
    
    private void SetWaypoint(MapIcon mapIcon, LineBehaviour lineBehaviour)
    {
        var waypoint = mapIcon.GetComponent<WaypointBehaviour>();
        if (waypoint == null)
        {
            Log.Warning($"WaypointBehaviour not found on {mapIcon.name}");
            Destroy(mapIcon);
        }
        else
        {
            if (lineBehaviour == null)
            {
                Log.Warning($"LineBehaviour not found on {mapIcon.name}");
            }
            else
            {
                waypoint.LineComp = lineBehaviour;
                lineBehaviour.SetContainer(lineContainer);
            }

            void OnRightClick() => RemovedWaypointByPlayer(mapIcon);
            waypoint.SetAction(OnRightClick);
            _waypoints.Add(mapIcon);
        }
        if (_waypoints.Count == 1)
        {
            OnRouteStarted?.Invoke();
        }
        else
        {
            OnRouteModified?.Invoke();
        }
    }
    
    private void RemovedWaypointByPlayer(MapIcon icon)
    {
        RemoveWaypoint(icon);
        OnRouteModified?.Invoke();
    }
    
    public void RemoveWaypointOnArrival()
    {
        RemoveWaypoint(_waypoints[0]);
    }
    
    private void RemoveWaypoint(MapIcon icon)
    {
        var point = icon.gameObject.GetComponent<WaypointBehaviour>();
        _waypoints.Remove(icon);
        DestroyWaypoint(point);
        RefreshIndices();
    }

    
    private void DestroyWaypoint(WaypointBehaviour waypointBehaviour)
    {
        waypointBehaviour.OnDestroyWaypoint();
    }

    #endregion
    
    #region RouteChecks

    private void RefreshIndices()
    {
        for (int i = 0; i < _waypoints.Count; i++)
        {
            var waypointComp = _waypoints[i].GetComponent<WaypointBehaviour>();
            if (waypointComp == null) continue;
            waypointComp.SetIndex(i + 1);
            switch (i)
            {
                case 0:
                    waypointComp.LineComp.SetTarget(_submarineRect, _waypoints[0].GetComponent<RectTransform>());
                    break;
                case > 0:
                    waypointComp.LineComp.SetTarget(_waypoints[i-1].GetComponent<RectTransform>(), _waypoints[i].GetComponent<RectTransform>());
                    break;
            }
        }
    }
    
    public IReadOnlyList<MapIcon> GetWaypoints() => _waypoints;
    
    #endregion
}