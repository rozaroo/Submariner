using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class WaypointManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Properties")] 
    [SerializeField] private MapAssetSO waypointPointConfig;
    [SerializeField] private GameObject lineContainer;
    
    [Header("Level Configuration")]
    [SerializeField] private MapRuntimeDataSO mapRuntimeData; 

    [Header("Anchors")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransformAnchorSO submarineRectAnchor; 

    [Header("Event Channels")]
    [SerializeField] private ListVector3EventChannelSO onSubmarineRouteChanged; 
    [SerializeField] private BaseEventChannelSO onSubmarineArrivedAtWaypoint;
    
    private readonly List<WaypointData> _waypoints = new();
    private RectTransform _mapRect;

    public event Action OnRouteStarted;
    public event Action OnRouteModified;
    
    private void OnEnable()
    {
        if (onSubmarineArrivedAtWaypoint != null)
            onSubmarineArrivedAtWaypoint.OnEventRaised += RemoveWaypointOnArrival;
    }

    private void OnDisable()
    {
        if (onSubmarineArrivedAtWaypoint != null)
            onSubmarineArrivedAtWaypoint.OnEventRaised -= RemoveWaypointOnArrival;
    }

    #region Pointer Events Handlers

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            HandleLeftClick(eventData);
    }

    private void HandleLeftClick(PointerEventData eventData)
    {
        if (submarineRectAnchor == null || submarineRectAnchor.Value == null || mapRect == null)
        {
            Log.Info($"[{name}]- No Map Rect or Submarine Anchor Assigned/Active");
            return;
        }
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRect,
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
        MapIcon mapIcon = MapIconFactory.Create(waypointPointConfig, mapRect);
        mapIcon.GetComponent<RectTransform>().anchoredPosition = point;
        return mapIcon;
    }
    
    private void SetWaypoint(MapIcon mapIcon, LineBehaviour lineBehaviour)
    {
        var data = new WaypointData 
        {
            Icon = mapIcon,
            Behaviour = mapIcon.GetComponent<WaypointBehaviour>(),
            Rect = mapIcon.GetComponent<RectTransform>()
        };
        
        if (data.Behaviour == null)
        {
            Log.Warning($"WaypointBehaviour not found on {mapIcon.name}");
            Destroy(mapIcon);
            return;
        }

        if (lineBehaviour != null)
        {
            data.Behaviour.LineComp = lineBehaviour;
            lineBehaviour.SetContainer(lineContainer);
        }

        void OnRightClick() => RemovedWaypointByPlayer(data);
        data.Behaviour.SetAction(OnRightClick);
        _waypoints.Add(data);

        if (_waypoints.Count == 1) OnRouteStarted?.Invoke();
        else OnRouteModified?.Invoke();
    }
    
    private void RemovedWaypointByPlayer(WaypointData data)
    {
        RemoveWaypoint(data);
        OnRouteModified?.Invoke();
    }
    
    public void RemoveWaypointOnArrival()
    {
        if (_waypoints.Count > 0)
        {
            RemoveWaypoint(_waypoints[0]);
            OnRouteModified?.Invoke();
        }
    }
    
    private void RemoveWaypoint(WaypointData data)
    {
        _waypoints.Remove(data);
        MapIconFactory.Release(data.Icon); 
        RefreshIndices();
    }

    #endregion
    
    #region RouteChecks

    private void RefreshIndices()
    {
        for (int i = 0; i < _waypoints.Count; i++)
        {
            var waypointComp = _waypoints[i].Behaviour;
            if (waypointComp == null) continue;
            waypointComp.SetIndex(i + 1);
            switch (i)
            {
                case 0:
                    if (submarineRectAnchor != null && submarineRectAnchor.Value != null)
                    {
                        waypointComp.LineComp.SetTarget(submarineRectAnchor.Value, _waypoints[0].Rect);
                    }
                    break;
                case > 0:
                    waypointComp.LineComp.SetTarget(_waypoints[i-1].Rect, _waypoints[i].Rect);
                    break;
            }
        }
        SendWorldRoute();
    }

    private void SendWorldRoute()
    {
        if (onSubmarineRouteChanged == null || mapRuntimeData == null) return;

        List<Vector3> worldWaypoints = new List<Vector3>();
        for (int i = 0; i < _waypoints.Count; i++)
        {
            Vector3 worldPos = WorldPositionConverter.MapToWorld(
                _waypoints[i].Rect.anchoredPosition,
                mapRuntimeData.worldMapSize,
                mapRuntimeData.uiMapSize
            );
            worldWaypoints.Add(worldPos);
        }
        onSubmarineRouteChanged.RaiseEvent(worldWaypoints);
    }
    
    public IReadOnlyList<WaypointData> GetWaypoints() => _waypoints;
    
    #endregion
}