using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class WaypointManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Properties")] 
    [SerializeField] private MapAssetSO waypointPointConfig;
    [SerializeField] private GameObject lineContainer;
    private RectTransform canvasRect;
    
    private readonly List<MapIcon> _waypoints = new();
    private RectTransform _submarineRect;
    public RectTransform SubmarineRect
    {
        set => _submarineRect = value;
    }
    public event Action OnRouteStarted;
    public event Action OnRouteCancelled;
    public event Action OnRouteModified;

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            HandleLeftClick(eventData);
    }

    private void HandleLeftClick(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        
        var mapIcon = CreateMapIcon(localPoint);
        var iconLineBehaviour = mapIcon.gameObject.GetComponent<LineBehaviour>();
        
        SetWaypoint(mapIcon, iconLineBehaviour);
        RefreshIndices();
    }

    private MapIcon CreateMapIcon(Vector2 point)
    {
        MapIcon mapIcon = MapIconFactory.Create(waypointPointConfig, canvasRect);
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
            Action onRightClick = () => RemoveWaypoint(mapIcon);
            waypoint.OnRightClicked += onRightClick;
            waypoint.SetAction(onRightClick);
            _waypoints.Add(mapIcon);
        }
    }

    private void RemoveWaypoint(MapIcon icon)
    {
        var point = icon.gameObject.GetComponent<WaypointBehaviour>();
        DestroyWaypoint(point);
        _waypoints.Remove(icon);
        Destroy(icon.gameObject);
        RefreshIndices();
    }

    public void RemoveWaypointOnArrival()
    {
        var point = _waypoints[0].gameObject.GetComponent<WaypointBehaviour>();
        DestroyWaypoint(point);
        _waypoints.Remove(_waypoints[0]);
        RefreshIndices();
    }

    private void DestroyWaypoint(WaypointBehaviour waypointBehaviour)
    {
        waypointBehaviour.OnDestroyWaypoint();
    }

    private void RefreshIndices()
    {
        for (int i = 0; i < _waypoints.Count; i++)
        {
            var waypointComp = _waypoints[i].GetComponent<WaypointBehaviour>();
            if (waypointComp != null)
            {
                waypointComp.SetIndex(i + 1);
                if (i == 0)
                {
                    waypointComp.LineComp.SetTarget(_submarineRect, _waypoints[0].GetComponent<RectTransform>());
                }
                if (i > 0)
                {
                    waypointComp.LineComp.SetTarget(_waypoints[i-1].GetComponent<RectTransform>(), _waypoints[i].GetComponent<RectTransform>());
                }
            }
        }
        if (_waypoints.Count <= 0)
        {
            OnRouteCancelled?.Invoke();
        }
        if (_waypoints.Count == 1)
        {
            OnRouteStarted?.Invoke();
        }
        if (_waypoints.Count > 1)
        {
            OnRouteModified?.Invoke();
        }
    }

    public IReadOnlyList<MapIcon> GetWaypoints() => _waypoints;
}