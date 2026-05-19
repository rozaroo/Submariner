using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapManager : MonoBehaviour
{
    [Header("Map Properties")] 
    [SerializeField] private GameObject mapIconContainer;
    [SerializeField] private Image backgroundImage;
    
    [Header("Map Event Properties")] 
    [SerializeField] private int desiredEventsAmount;
    [SerializeField] private int mapIconSpawnAttempts;
    [SerializeField] private float mapSize;
    
    [Header("Icons SO")]
    [SerializeField] private MapAssetSO eventIconSo;
    [SerializeField] private MapAssetSO interestPointSo;
    [SerializeField] private MapAssetSO objectivePointSo;
    [SerializeField] private MapAssetSO submarineSo;

    private Canvas _mapCanvas;
    private RectTransform _mapRect;
    private Submarine2DMovementBehaviour _mapSubmarine;
    private WaypointManager _waypointManager;
    private List<MapIcon> _mapIcons;
    public Canvas MapCanvas => _mapCanvas;
    
    private void Awake()
    {
        _mapIcons = new List<MapIcon>();
        _mapCanvas = GetComponent<Canvas>();
        _waypointManager = GetComponent<WaypointManager>();
        _mapRect = mapIconContainer.GetComponent<RectTransform>();
        ChangeMapSize();
        if (_mapRect != null)
        {
            _waypointManager.MapRect = _mapRect;
        }
    }

    private void OnEnable()
    {
        _waypointManager.OnRouteStarted += OnStartTravelingSubmarine;
        _waypointManager.OnRouteModified += OnUpdateSubmarineRoute;
    }

    private void OnDisable()
    {
        _waypointManager.OnRouteStarted -= OnStartTravelingSubmarine;
        _waypointManager.OnRouteModified -= OnUpdateSubmarineRoute;
    }

    #region MapUtilities
    
    [ContextMenu("Map Generation/Generate Map")]
    public void GenerateMap() //TODO: Change to Private once is working and tested via ContextMenu.
    {
        int removedIcons = 0;
        int iconNumber = 0;
        List<MapIcon> generatedIcons = new List<MapIcon>();
        
        for (int i = 0; i < desiredEventsAmount; i++)
        {
            var icon = GenerateDesiredIcon(eventIconSo);
                icon.gameObject.SetActive(false);
            generatedIcons.Add(icon);
        }
        
        foreach (var icon in generatedIcons)
        {
            if (TryAssignPosition(icon.IconRectTransform))
            {
                icon.gameObject.SetActive(true);
                _mapIcons.Add(icon);
                iconNumber++;
                Log.Info($"Icon Number: {iconNumber} - Spawning at {icon.IconRectTransform.anchoredPosition}");
            }
            else
            {
                Destroy(icon.gameObject);
                removedIcons++;
            }
        }
        Log.Info($"Removed {removedIcons} icons from {_mapIcons.Count}.");
    }
    
    [ContextMenu("Map Generation/Create Submarine")]
    public void GenerateSubmarine()
    {
        if (submarineSo == null || _mapSubmarine != null) return;
        var submarine = GenerateDesiredIcon(submarineSo);
        submarine.IconRectTransform.anchoredPosition = GenerateIconLocation(submarine.IconRectTransform, GetScaleRange(_mapRect));
        _mapSubmarine = submarine.GetComponent<Submarine2DMovementBehaviour>();
        
        if (_mapSubmarine == null) return;
        
        Action reachWaypointAction = OnReachRemoveWaypoint;
        _mapSubmarine.SetWaypointReachedAction(reachWaypointAction);
        
        _waypointManager.SubmarineRect = submarine.IconRectTransform;
    }
    
    private void ChangeMapSize()
    {
        if (backgroundImage != null)
        {
            backgroundImage.rectTransform.sizeDelta = new Vector2(mapSize, mapSize);
        }
        if (_mapRect != null)
        {
            _mapRect.sizeDelta = new Vector2(mapSize, mapSize);
        }
    }
    
    private MapIcon GenerateDesiredIcon(MapAssetSO mapAsset)
    {
        var iconCreated = MapIconFactory.Create(mapAsset, _mapRect);
        return iconCreated;
    }
    
    //NOTE: THIS ONLY WORKS IF THE ICON IS A SQUARE. MODIFY IN THE FUTURE.
    private bool TryAssignPosition(RectTransform iconRect)
    {
        if (_mapIcons.Count > 0)
        {
            for (var i = 0; i <= mapIconSpawnAttempts; i++)
            {
                var desiredPosition = GenerateIconLocation(iconRect, GetScaleRange(_mapRect));
                if (TrySetIconLocation(iconRect, desiredPosition))
                {
                    iconRect.anchoredPosition = desiredPosition;
                    return true;
                }
            }
        }
        else
        {
            iconRect.anchoredPosition = GenerateIconLocation(iconRect, GetScaleRange(_mapRect));
            return true;
        }
        return false;
    }

    private bool TrySetIconLocation(RectTransform icon, Vector2 desiredPosition)
    {
        foreach (var mapIcon in _mapIcons)
        {
            if (CheckForOverlap(icon, mapIcon.IconRectTransform, desiredPosition))
            {
                return false;
            }
        }
        return true;
    }

    private bool CheckForOverlap(RectTransform icon1, RectTransform icon2, Vector2 desiredPosition)
    {
        var iconScale1 = GetScaleRange(icon1);
        var iconScale2 = GetScaleRange(icon2);
                
        var scalar = iconScale1 + iconScale2;
        var minimalDistance = scalar.magnitude;
        
        var centerDistance = Vector2.Distance(desiredPosition, icon2.anchoredPosition);
        return centerDistance < minimalDistance; //If True, OVERLAP!.
    }

    private Vector2 GenerateIconLocation(RectTransform icon, Vector2 mapScale)
    {
        var iconPosibleLocations =
            new Vector2(mapScale.x - icon.rect.width / 2, mapScale.y - icon.rect.height / 2);

        var coordinatesX = Random.Range(-iconPosibleLocations.x, iconPosibleLocations.x);
        var coordinatesY = Random.Range(-iconPosibleLocations.y, iconPosibleLocations.y);

        var desiredPosition = new Vector2(coordinatesX, coordinatesY);
        return desiredPosition;
    }

    private Vector2 GetScaleRange(RectTransform subject)
    {
        var containerScale = new Vector2(subject.rect.width / 2, subject.rect.height / 2);
        return containerScale;
    }
    
    #endregion

    #region WaypointCommunicationUtilities

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

    #endregion
}