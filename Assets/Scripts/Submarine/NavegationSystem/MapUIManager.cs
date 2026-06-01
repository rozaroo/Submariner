using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapUIManager : MonoBehaviour
{
    [Header("Map Properties")] 
    [SerializeField] private GameObject mapIconContainer;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private float mapSize;
    
    [Header("World Map Sync")]
    [SerializeField] private float linearSyncSpeed = 100f;

    [Header("Event Channel")]
    [SerializeField] private WorldMapGeneratedPropertyEventChannelSO onWorldMapGenerated;
    [SerializeField] private WorldMapUIElementEventChannelSO onWorldSubmarineElementGenerated;
    [SerializeField] private WorldMapUIElementEventChannelSO onWorldElementGenerated;
    
    private Canvas _mapCanvas;
    private RectTransform _mapRect;
    private SubmarineMovement _mapSubmarine;
    private WaypointManager _waypointManager; //TODO: Maybe use Event Channels for this instead of direct reference.
    private Coroutine _syncCoroutine;
    public Canvas MapCanvas => _mapCanvas;
    
    private bool _isSyncing;
    private float _worldMapSize;
    private Dictionary<IWorldMapUIElement, MapIcon> _worldElementIconDictionary; //To store every icon created, but maybe separate them by update mode to avoid iterating through static icons when syncing dynamic ones.
    private Dictionary<IWorldMapUIElement, DynamicIconData> _dynamicIconDictionary;
    
    private void Awake()
    {
        _worldElementIconDictionary = new Dictionary<IWorldMapUIElement, MapIcon>(); 
        _dynamicIconDictionary = new Dictionary<IWorldMapUIElement, DynamicIconData>();
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
        onWorldMapGenerated.OnEventRaised += OnMapUpdated;
        onWorldSubmarineElementGenerated.OnEventRaised += OnSubmarineGenerated;
        onWorldElementGenerated.OnEventRaised += OnWorldElementGenerated;
        InitializeMapSync();
        
        _waypointManager.OnRouteStarted += OnStartTravelingSubmarine;
        _waypointManager.OnRouteModified += OnUpdateSubmarineRoute;
    }

    private void OnDisable()
    {
        onWorldMapGenerated.OnEventRaised -= OnMapUpdated;
        onWorldSubmarineElementGenerated.OnEventRaised -= OnSubmarineGenerated;
        onWorldElementGenerated.OnEventRaised -= OnWorldElementGenerated;
        StopMapSync();
        
        if (_mapSubmarine != null)
            _mapSubmarine.OnWaypointReached -= OnReachRemoveWaypoint;
        
        _waypointManager.OnRouteStarted -= OnStartTravelingSubmarine;
        _waypointManager.OnRouteModified -= OnUpdateSubmarineRoute;
    }

    #region MapUtilities
    
    private void OnMapUpdated(WorldMapGeneratedProperty worldMapProperties)
    {
        _worldMapSize = worldMapProperties.WorldSize;
        ChangeMapSize();
        ClearAllMapIcons();
        foreach (var element in worldMapProperties.mapElements)
        {
            OnWorldElementGenerated(element);
        }
    }
    
    private void OnWorldElementGenerated(IWorldMapUIElement element)
    {
        MapIcon generatedIcon = GenerateDesiredIcon(element);
        SetIconPosition(element,generatedIcon);
    }
    
    private MapIcon GenerateDesiredIcon(IWorldMapUIElement element)
    {
        if (element != null)
        {
            MapIcon iconCreated = MapIconFactory.Create(element.MapAsset,_mapRect);
            if (iconCreated != null)
            {
                element.OnElementDestroyed += OnElementDestroyed;
                _worldElementIconDictionary.Add(element, iconCreated);
                StoreViaUpdateMode(element);
                return iconCreated;
            }
        }
        return null;
    }
    
    private void OnSubmarineGenerated(IWorldMapUIElement element)
    {
        Log.Info($"[OnSubmarineGenerated] _worldMapSize: {_worldMapSize}");
        MapIcon icon = GenerateDesiredIcon(element);
        if (icon == null) return;
    
        SetIconPosition(element, icon);
        SetIconRotation(element, icon);
        
        _waypointManager.SubmarineRect = icon.IconRectTransform;
        _mapSubmarine = (element as MonoBehaviour)?.GetComponent<SubmarineMovement>();
        
        if (_mapSubmarine != null)
            _mapSubmarine.OnWaypointReached += OnReachRemoveWaypoint;
    }
    
    private void StoreViaUpdateMode(IWorldMapUIElement element)
    {
        switch (element.UpdateMode)
        {
            case WorldUIUpdateMode.Static:
                break;
            case WorldUIUpdateMode.Dynamic:
                _dynamicIconDictionary.Add(element, new DynamicIconData 
                { 
                    Icon = _worldElementIconDictionary[element],
                    PositionVelocity = Vector2.zero,
                    SyncTime = element.SyncTime
                });
                if (!_isSyncing)
                {
                    InitializeMapSync();
                }
                break;
            default:
                Log.Warning($"[MapUIManager] Unhandled Update Mode Type: {element.UpdateMode}");
                break;
        }
    }
    
    private void SetIconPosition(IWorldMapUIElement element, MapIcon icon)
    {
        icon.IconRectTransform.anchoredPosition = 
            WorldPositionConverter.WorldToMap(element.Position, _worldMapSize, mapSize);
    }
    
    private void SetIconRotation(IWorldMapUIElement element, MapIcon icon)
    {
        Log.Info(element.Rotation.y.ToString());
        icon.IconRectTransform.localRotation = Quaternion.Euler(0,0,element.Rotation.y);
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
    
    private void ClearAllMapIcons()
    {
        foreach (var pair in _worldElementIconDictionary)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }
        _worldElementIconDictionary.Clear();
    }
    
    private void OnElementDestroyed(IWorldMapUIElement element)
    {
        element.OnElementDestroyed -= OnElementDestroyed;
        
        if (_worldElementIconDictionary.TryGetValue(element, out MapIcon icon))
            if(icon != null)
                Destroy(icon.gameObject);
        
        _worldElementIconDictionary.Remove(element);
        _dynamicIconDictionary.Remove(element);
    }
    #endregion

    #region MapSync

    [ContextMenu("Initialize Map Sync")]
    private void InitializeMapSync()
    {
        if (_syncCoroutine != null)
        {
            StopCoroutine(_syncCoroutine);
        }
        _syncCoroutine = StartCoroutine(MapDynamicSyncCoroutine());
    }

    [ContextMenu("Stop Map Sync")]
    private void StopMapSync()
    {
        if (_syncCoroutine != null)
        {
            _isSyncing = false;
            StopCoroutine(_syncCoroutine);
            _syncCoroutine = null;
        }
    }
    
    private IEnumerator MapDynamicSyncCoroutine()
    {
        _isSyncing = true;
        while (_isSyncing && _dynamicIconDictionary.Count > 0)
        {
            foreach (var pair in _dynamicIconDictionary)
            {
                SyncIconPosition(pair.Key, pair.Value);
                SyncIconRotation(pair.Key, pair.Value);
            }
            yield return null;
        }
        _isSyncing = false;
    }

    private void SyncIconPosition(IWorldMapUIElement element, DynamicIconData data)
    {
        Vector2 targetPos = WorldPositionConverter.WorldToMap(
            element.Position, _worldMapSize, mapSize);
    
        switch (element.SyncMode)
        {
            case WorldUISyncMode.Linear:
                data.Icon.IconRectTransform.anchoredPosition = Vector2.MoveTowards(
                    data.Icon.IconRectTransform.anchoredPosition,
                    targetPos,
                    linearSyncSpeed * Time.deltaTime);
                break;
            case WorldUISyncMode.Smooth:
                data.Icon.IconRectTransform.anchoredPosition = Vector2.SmoothDamp(
                    data.Icon.IconRectTransform.anchoredPosition,
                    targetPos,
                    ref data.PositionVelocity,
                    data.SyncTime);
                break;
        }
    }
    
    private void SyncIconRotation(IWorldMapUIElement element, DynamicIconData data)
    {
        float targetAngle = -element.Rotation.y;
    
        switch (element.SyncMode)
        {
            case WorldUISyncMode.Linear:
                data.CurrentAngle = Mathf.MoveTowardsAngle(
                    data.CurrentAngle, targetAngle, linearSyncSpeed * Time.deltaTime);
                break;
            case WorldUISyncMode.Smooth:
                data.CurrentAngle = Mathf.SmoothDampAngle(
                    data.CurrentAngle, targetAngle,
                    ref data.RotationVelocity, data.SyncTime);
                break;
        }
        data.Icon.IconRectTransform.localRotation = Quaternion.Euler(0, 0, data.CurrentAngle);
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
    
        if (waypoints.Count == 0)
        {
            _mapSubmarine.StopMovingTowards();
            return;
        }
    
        List<Vector3> worldWaypoints = waypoints
            .Select(icon => WorldPositionConverter.MapToWorld(
                icon.IconRectTransform.anchoredPosition,
                _worldMapSize,
                mapSize))
            .ToList();
        _mapSubmarine.GetNewWaypointList(worldWaypoints);
    }

    #endregion

    #region Testing

    [ContextMenu("Check Coroutines")]
    public void CheckCoroutines()
    {
        if (_syncCoroutine != null)
        {
            Log.Info("[MapUIManager] Sync Coroutine is active.");
        }
        else
        {
            Log.Info("[MapUIManager] Sync Coroutine is not active.");
        }
    }

    [ContextMenu("Check Icons")]
    public void CheckIcons()
    {
        Log.Info($"[MapUIManager] Total Icons: {_worldElementIconDictionary.Count}, Dynamic Icons: {_dynamicIconDictionary.Count}");
    }
    #endregion
}