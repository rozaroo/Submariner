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
    
    [Header("Level Configuration")]
    [SerializeField] private MapRuntimeDataSO mapRuntimeData;
    
    [Header("World Map Sync")]
    [SerializeField] private float linearSyncSpeed = 100f;

    [Header("Event Channels")]
    [SerializeField] private SonarElementsDetectionEventChannelSO onOuterRadarChanged;
    [SerializeField] private SonarElementsDetectionEventChannelSO onInnerRadarChanged;
    [SerializeField] private WorldMapGeneratedPropertyEventChannelSO onWorldMapGenerated;
    [SerializeField] private WorldMapUIElementEventChannelSO onWorldSubmarineElementGenerated;
    [SerializeField] private WorldMapUIElementEventChannelSO onWorldElementGenerated;

    private RectTransform _mapRect;
    private Coroutine _syncCoroutine;
    private bool _isSyncing;
    
    private Dictionary<IWorldMapUIElement, MapIcon> _worldElementIconDictionary; //To store every icon created, but maybe separate them by update mode to avoid iterating through static icons when syncing dynamic ones.
    private Dictionary<IWorldMapUIElement, DynamicIconData> _dynamicIconDictionary;
    
    public Canvas MapCanvas { get; private set; }

    private void Awake()
    {
        MapCanvas = GetComponent<Canvas>();
        _worldElementIconDictionary = new Dictionary<IWorldMapUIElement, MapIcon>(); 
        _dynamicIconDictionary = new Dictionary<IWorldMapUIElement, DynamicIconData>();
        _mapRect = mapIconContainer.GetComponent<RectTransform>();
        ChangeMapSize();
    }

    private void OnEnable()
    {
        onWorldMapGenerated.OnEventRaised += OnMapUpdated;
        onWorldSubmarineElementGenerated.OnEventRaised += OnWorldElementGenerated;
        onWorldElementGenerated.OnEventRaised += OnWorldElementGenerated;
        
        if (onOuterRadarChanged != null) onOuterRadarChanged.OnEventRaised += OnOuterRadarChanged;
        if (onInnerRadarChanged != null) onInnerRadarChanged.OnEventRaised += OnInnerRadarChanged;
        
        InitializeMapSync();
    }

    private void OnDisable()
    {
        onWorldMapGenerated.OnEventRaised -= OnMapUpdated;
        onWorldSubmarineElementGenerated.OnEventRaised -= OnWorldElementGenerated;
        onWorldElementGenerated.OnEventRaised -= OnWorldElementGenerated;
        
        if (onOuterRadarChanged != null) onOuterRadarChanged.OnEventRaised -= OnOuterRadarChanged;
        if (onInnerRadarChanged != null) onInnerRadarChanged.OnEventRaised -= OnInnerRadarChanged;
        
        StopMapSync();
    }
    
    #region MapUtilities
    
    private void OnMapUpdated(WorldMapGeneratedProperty worldMapProperties)
    {
        ChangeMapSize();
        ClearAllMapIcons();
        foreach (var element in worldMapProperties.mapElements)
        {
            OnWorldElementGenerated(element);
        }
    }
    
    private void OnWorldElementGenerated(IWorldMapUIElement element)
    {
        if (_worldElementIconDictionary.ContainsKey(element)) return;

        MapIcon generatedIcon = GenerateDesiredIcon(element);
        if (generatedIcon != null)
        {
            SetIconPosition(element, generatedIcon);
            SetIconRotation(element, generatedIcon);
        }
    }
    
    private MapIcon GenerateDesiredIcon(IWorldMapUIElement element)
    {
        MapIcon iconCreated = MapIconFactory.Create(element.MapAsset, _mapRect);
        if (iconCreated != null)
        {
            iconCreated.IsVisible = element.MapAsset.startsVisible; 
            iconCreated.BindToWorldEntity(element); // <--- Aquí se ejecuta el auto-registro del radar y del anchor del submarino
            element.OnEntityDestroyed += OnElementDestroyed;
            _worldElementIconDictionary.Add(element, iconCreated);
            StoreViaUpdateMode(element);
            return iconCreated;
        }
        return null;
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
            WorldPositionConverter.WorldToMap(element.Position, mapRuntimeData.worldMapSize, mapRuntimeData.uiMapSize);
    }
    
    private void SetIconRotation(IWorldMapUIElement element, MapIcon icon)
    {
        icon.IconRectTransform.localRotation = Quaternion.Euler(0,0,element.Rotation.y);
    }
    
    private void ChangeMapSize()
    {
        if(mapRuntimeData == null)
        {
            Log.Error("[MapUIManager] Map Runtime Data is not assigned.");
        }
        else
        {
            if (backgroundImage != null)
            {
                backgroundImage.rectTransform.sizeDelta = new Vector2(mapRuntimeData.uiMapSize, mapRuntimeData.uiMapSize);
            }
            if (_mapRect != null)
            {
                _mapRect.sizeDelta = new Vector2(mapRuntimeData.uiMapSize, mapRuntimeData.uiMapSize);
            }
        }
    }
    
    private void ClearAllMapIcons()
    {
        foreach (var pair in _worldElementIconDictionary)
        {
            if (pair.Value != null)
            {
                MapIconFactory.Release(pair.Value); 
            }
        }
        _worldElementIconDictionary.Clear();
    }
    
    private void OnElementDestroyed(IWorldElement entity)
    {
        entity.OnEntityDestroyed -= OnElementDestroyed;
        
        if (entity is IWorldMapUIElement uiElement)
        {
            if (_worldElementIconDictionary.TryGetValue(uiElement, out MapIcon icon))
            {
                if(icon != null)
                {
                    MapIconFactory.Release(icon);
                }
            }
            _worldElementIconDictionary.Remove(uiElement);
            _dynamicIconDictionary.Remove(uiElement);
        }
    }
    #endregion

    #region MapSyncUtilities

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
            element.Position, mapRuntimeData.worldMapSize, mapRuntimeData.uiMapSize);
    
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

    #region SonarCommunicationUtilities

    private void OnOuterRadarChanged(SonarElementsDetectionProperty property)
    {
        if (property.WorldElement is IWorldMapUIElement uiElement)
        {
            if (_worldElementIconDictionary.TryGetValue(uiElement, out MapIcon icon))
            {
                icon.IsVisible = property.IsRevealed;
            }
        }
    }

    private void OnInnerRadarChanged(SonarElementsDetectionProperty property)
    {
        if (property.WorldElement is IWorldMapUIElement uiElement)
        {
            if (_worldElementIconDictionary.TryGetValue(uiElement, out MapIcon icon))
            {
                icon.IsVisible = !property.IsRevealed;
            }
        }
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