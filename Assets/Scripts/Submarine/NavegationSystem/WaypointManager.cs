using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WaypointManager : MonoBehaviour
{
    [Header("Properties")] 
    [SerializeField] private MapAssetSO waypointPointConfig;
    [SerializeField] private GameObject lineContainer;
    
    [Header("Level Configuration")]
    [SerializeField] private MapRuntimeDataSO mapRuntimeData; 

    [Header("Anchors")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransformAnchorSO submarineRectAnchor;
    
    [Header("Interaction")]
    [SerializeField] private NavigationStation navigationStation;
    
    [Header("Interaction Settings")]
    [Tooltip("Radius of Pixels to interact with.")]
    [SerializeField] private float removalRadius = 30f;
    
    private readonly List<WaypointData> _waypoints = new();
    private RectTransform _mapRect;
    
    private void OnEnable()
    {
        GameEventChannel<OnSubmarineArrivedAtCheckpoint>.OnEventRaised += RemoveWaypointOnArrival;
        GameEventChannel<OnSubmarineRouteCleared>.OnEventRaised += RemoveAllWaypoints;
    }

    private void OnDisable()
    {
        GameEventChannel<OnSubmarineArrivedAtCheckpoint>.OnEventRaised -= RemoveWaypointOnArrival;
        GameEventChannel<OnSubmarineRouteCleared>.OnEventRaised -= RemoveAllWaypoints;
    }

    #region Pointer Events Handlers

    private void Update()
    {
        if (Mouse.current == null) return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMapClick(true);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleMapClick(false);
        }
    }

    private void HandleMapClick(bool isLeftClick)
    {
        if (submarineRectAnchor == null || submarineRectAnchor.Value == null || mapRect == null) return;

        if (navigationStation == null || navigationStation.ActiveCamera == null) return;

        Camera playerCamera = navigationStation.ActiveCamera;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 viewportPos = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
        Ray ray = playerCamera.ViewportPointToRay(viewportPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            if (hit.collider.gameObject == navigationStation.gameObject)
            {
                Vector3 localPoint3D = mapRect.InverseTransformPoint(hit.point);
                Vector2 localPoint2D = new Vector2(localPoint3D.x, localPoint3D.y);

                if (isLeftClick)
                {
                    var mapIcon = CreateMapIcon(localPoint2D);
                    var iconLineBehaviour = mapIcon.gameObject.GetComponent<LineBehaviour>();

                    SetWaypoint(mapIcon, iconLineBehaviour);
                    RefreshIndices();
                }
                else
                {
                    TryRemoveWaypointAt(localPoint2D);
                }
            }
        }
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
            Log.Warning($"WaypointBehaviour not found on {mapIcon}");
            Destroy(mapIcon);
            return;
        }

        if (lineBehaviour != null)
        {
            data.Behaviour.LineComp = lineBehaviour;
            lineBehaviour.SetContainer(lineContainer);
        }
        _waypoints.Add(data);
    }
    
    private void TryRemoveWaypointAt(Vector2 clickLocalPosition)
    {
        for (int i = _waypoints.Count - 1; i >= 0; i--)
        {
            float distance = Vector2.Distance(clickLocalPosition, _waypoints[i].Rect.anchoredPosition);
            
            if (distance <= removalRadius)
            {
                RemovedWaypointByPlayer(_waypoints[i]);
                return;
            }
        }
    }
    
    private void RemovedWaypointByPlayer(WaypointData data)
    {
        RemoveWaypoint(data);
    }
    
    private void RemoveWaypointOnArrival(OnSubmarineArrivedAtCheckpoint data)
    {
        if (_waypoints.Count > 0)
        {
            RemoveWaypoint(_waypoints[0]);
        }
    }
    
    private void RemoveWaypoint(WaypointData data)
    {
        _waypoints.Remove(data);
        MapIconFactory.Release(data.Icon); 
        RefreshIndices();
    }

    private void RemoveAllWaypoints(OnSubmarineRouteCleared data)
    {
        for (int i = _waypoints.Count - 1; i >= 0; i--)
        {
            MapIconFactory.Release(_waypoints[i].Icon);
        }
        _waypoints.Clear();
        RefreshIndices();
        Log.Info("[Waypoint Manager] Route Cleaned  ");
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
        if (mapRuntimeData == null) return;

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
        GameEventChannel<OnSubmarineRouteChanged>.RaiseEvent(new OnSubmarineRouteChanged(worldWaypoints));
    }
    
    #endregion
}