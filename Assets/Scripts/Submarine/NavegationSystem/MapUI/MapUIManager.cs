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
    
    [Header("Audio Settings")]
    [SerializeField] private float pingAudioCooldown = 0.1f;

    private float _lastOuterPingTime = -100f;
    private float _lastInnerPingTime = -100f;
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
        GameEventChannel<OnWorldMapGeneratedProperty>.OnEventRaised += OnMapUpdated;
        GameEventChannel<OnWorldSubmarineGenerated>.OnEventRaised += OnWorldSubmarineGenerated;
        GameEventChannel<OnWorldMapElementGenerated>.OnEventRaised += OnWorldElementGenerated;
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised += OnSonarChanged;
        
        InitializeMapSync();
    }

    private void OnDisable()
    {
        GameEventChannel<OnWorldMapGeneratedProperty>.OnEventRaised -= OnMapUpdated;
        GameEventChannel<OnWorldSubmarineGenerated>.OnEventRaised -= OnWorldSubmarineGenerated;
        GameEventChannel<OnWorldMapElementGenerated>.OnEventRaised -= OnWorldElementGenerated;
        
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised -= OnSonarChanged;
        
        StopMapSync();
    }
    
    #region MapUtilities
    
    private void OnMapUpdated(OnWorldMapGeneratedProperty onWorldMapProperties)
    {
        ChangeMapSize();
        ClearAllMapIcons();
        foreach (var element in onWorldMapProperties.MapElements)
        {
            OnWorldElementGenerated(new OnWorldMapElementGenerated(element));
        }
    }
    
    private void OnWorldElementGenerated(OnWorldMapElementGenerated element)
    {
        if (_worldElementIconDictionary.ContainsKey(element._worldElementGenerated)) return;

        MapIcon generatedIcon = GenerateDesiredIcon(element._worldElementGenerated);
        if (generatedIcon != null)
        {
            SetIconPosition(element._worldElementGenerated, generatedIcon);
            SetIconRotation(element._worldElementGenerated, generatedIcon);
        }
    }
    
    private void OnWorldSubmarineGenerated(OnWorldSubmarineGenerated property)
    {
        if (property._submarineElement != null)
        {
            OnWorldElementGenerated(new OnWorldMapElementGenerated(property._submarineElement));
        }
    }
    
    private MapIcon GenerateDesiredIcon(IWorldMapUIElement element)
    {
        MapIcon iconCreated = MapIconFactory.Create(element.mapAsset, _mapRect);
        if (iconCreated != null)
        {
            iconCreated.IsVisible = element.mapAsset.startsVisible; 
            iconCreated.BindToWorldEntity(element);
            element.OnElementDestroyed += OnElementDestroyed;
            _worldElementIconDictionary.Add(element, iconCreated);
            StoreViaUpdateMode(element);
            return iconCreated;
        }
        return null;
    }
    
    private void StoreViaUpdateMode(IWorldMapUIElement element)
    {
        switch (element.updateMode)
        {
            case WorldUIUpdateMode.Static:
                break;
            case WorldUIUpdateMode.Dynamic:
                _dynamicIconDictionary.Add(element, new DynamicIconData 
                { 
                    Icon = _worldElementIconDictionary[element],
                    PositionVelocity = Vector2.zero,
                    SyncTime = element.syncTime
                });
                if (!_isSyncing)
                {
                    InitializeMapSync();
                }
                break;
            default:
                Log.Warning($"[MapUIManager] Unhandled Update Mode Type: {element.updateMode}");
                break;
        }
    }
    
    private void SetIconPosition(IWorldMapUIElement element, MapIcon icon)
    {
        icon.IconRectTransform.anchoredPosition = 
            WorldPositionConverter.WorldToMap(element.position, mapRuntimeData.worldMapSize, mapRuntimeData.uiMapSize);
    }
    
    private void SetIconRotation(IWorldMapUIElement element, MapIcon icon)
    {
        icon.IconRectTransform.localRotation = Quaternion.Euler(0,0,element.rotation.y);
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
        entity.OnElementDestroyed -= OnElementDestroyed;
        
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
            element.position, mapRuntimeData.worldMapSize, mapRuntimeData.uiMapSize);
    
        switch (element.syncMode)
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
        float targetAngle = -element.rotation.y;
    
        switch (element.syncMode)
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

    private void OnSonarChanged(OnSonarElementsDetection property)
    {
        if (property.WorldElement is IWorldMapUIElement uiElement)
        {
            if (property.SonarRegion == SonarDetectionMode.OuterOnly)
            {
                if (_worldElementIconDictionary.TryGetValue(uiElement, out MapIcon icon))
                {
                    if (Time.time > _lastOuterPingTime + pingAudioCooldown)
                    {
                        SFXManager.PostEvent("Start_SonarPingOuter", gameObject);
                        _lastOuterPingTime = Time.time;
                    }
                    icon.IsVisible = property.IsRevealed;
                }
            }
            else if (property.SonarRegion == SonarDetectionMode.InnerOnly)
            {
                if (_worldElementIconDictionary.TryGetValue(uiElement, out MapIcon icon))
                {
                    if (Time.time > _lastOuterPingTime + pingAudioCooldown)
                    {
                        SFXManager.PostEvent("Start_SonarPingInner", gameObject);
                        _lastInnerPingTime = Time.time;
                    }
                    icon.IsVisible = !property.IsRevealed;
                }
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