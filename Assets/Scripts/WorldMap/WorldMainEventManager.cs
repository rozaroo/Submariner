using System.Collections.Generic;
using UnityEngine;

public class WorldMainEventManager : MonoBehaviour
{
    [Header("Extraction Point")]
    [SerializeField] private MapAssetSO extractionMapAsset;

    [Header("Radar Indicator")]
    [SerializeField] private float mainEventIndicatorRadius = 15f;

    [Header("Pacing")]
    [SerializeField] private float delayBetweenEvents = 1.5f;

    private List<MainWorldEvent> _mainEvents = new List<MainWorldEvent>();
    private ExtractionPointElement _extractionPoint;
    private int _currentIndex;
    private bool _initialized;
    
    private bool _isWaitingToAdvance;
    private float _advanceTimer;

    private void OnEnable()
    {
        GameEventChannel<OnMainEventsGenerated>.OnEventRaised += OnMainEventsGenerated;
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised += OnRadarStateChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnMainEventsGenerated>.OnEventRaised -= OnMainEventsGenerated;
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised -= OnRadarStateChanged;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (_isWaitingToAdvance)
        {
            _advanceTimer -= Time.deltaTime;
            if (_advanceTimer <= 0f)
            {
                _isWaitingToAdvance = false;
                AdvanceToNextEvent();
            }
        }
    }

    private void OnMainEventsGenerated(OnMainEventsGenerated data)
    {
        _mainEvents = data.MainEvents ?? new List<MainWorldEvent>();
        _extractionPoint = data.ExtractionPoint;
        _currentIndex = 0;
        _initialized = false;
        _isWaitingToAdvance = false;

        if (_mainEvents.Count == 0)
        {
            Log.Warning("[MainEventManager] Not generated Main Events.");
            return;
        }

        _initialized = true;
        BroadcastCurrentArea();
        Log.Info($"[MainEventManager] Initialized with {_mainEvents.Count} Main Events.");
    }

    private void OnRadarStateChanged(OnSonarElementsDetection property)
    {
        if (!_initialized) return;
        if (!property.IsRevealed) return;
        if (property.SonarRegion != SonarDetectionMode.InnerOnly) return;
        if (_currentIndex >= _mainEvents.Count) return;
        if (_isWaitingToAdvance) return;
        
        if (!ReferenceEquals(property.WorldElement, _mainEvents[_currentIndex])) return;

        IMainWorldEvent current = _mainEvents[_currentIndex];
        if (!current.CheckConditions()) return;

        current.Execute();
        StartAdvanceDelay();
    }

    private void StartAdvanceDelay()
    {
        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(Vector3.zero, 0f, false, string.Empty));

        _advanceTimer = delayBetweenEvents;
        _isWaitingToAdvance = true;
    }

    private void AdvanceToNextEvent()
    {
        _currentIndex++;
        if (_currentIndex >= _mainEvents.Count)
            UnlockExtractionPoint();
        else
            BroadcastCurrentArea();
    }

    private void BroadcastCurrentArea()
    {
        if (_currentIndex >= _mainEvents.Count) return;
        
        //Safety Check
        for (int i = 0; i < _currentIndex; i++) 
        {
            if (_mainEvents[i] != null && _mainEvents[i].gameObject.activeSelf)
            {
                Log.Warning($"[MainEventManager] Main Event {i} was still active. Forcing deactivation.");
                _mainEvents[i].gameObject.SetActive(false);
            }
        }

        MainWorldEvent currentEvent = _mainEvents[_currentIndex];
        
        currentEvent.gameObject.SetActive(true);
        
        GameEventChannel<OnWorldMapElementGenerated>.RaiseEvent(
            new OnWorldMapElementGenerated(currentEvent));
        
        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(currentEvent.position, mainEventIndicatorRadius, true, currentEvent.ObjectiveDescription));
        
        Log.Info($"[MainEventManager] Objective: Main Event {_currentIndex + 1}/{_mainEvents.Count}");
    }

    private void UnlockExtractionPoint()
    {
        if (_extractionPoint == null)
        {
            Log.Warning("[MainEventManager] Extraction Point not available.");
            return;
        }

        _extractionPoint.Setup(
            SonarDetectionMode.Both, extractionMapAsset,
            WorldUIUpdateMode.Static, WorldUISyncMode.Linear);

        _extractionPoint.gameObject.SetActive(true);
        _extractionPoint.OnSubmarineReachedExtraction += OnSubmarineReachedExtraction;
        
        GameEventChannel<OnWorldMapElementGenerated>.RaiseEvent(
            new OnWorldMapElementGenerated(_extractionPoint));
        
        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(_extractionPoint.position, mainEventIndicatorRadius, true, "Evacuation Process."));

        GameEventChannel<OnExtractionPointUnlocked>.RaiseEvent(new OnExtractionPointUnlocked());
        Log.Info("[MainEventManager] Extraction Point Unlocked.");
    }

    private void OnSubmarineReachedExtraction()
    {
        _extractionPoint.OnSubmarineReachedExtraction -= OnSubmarineReachedExtraction;

        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(Vector3.zero, 0f, false, string.Empty));

        GameEventChannel<OnGameWon>.RaiseEvent(new OnGameWon());
        Log.Info("[MainEventManager] Game Won");
    }

    #region Testing
    [ContextMenu("Force Advance Main Event")]
    private void DebugAdvance()
    {
        if (!_initialized || _currentIndex >= _mainEvents.Count) return;
        _mainEvents[_currentIndex].Execute();
        StartAdvanceDelay();
    }

    [ContextMenu("Check State")]
    private void DebugState()
    {
        Log.Info($"[MainEventManager] Current Event: {_currentIndex}/{_mainEvents.Count} | Initialized: {_initialized} | Waiting: {_isWaitingToAdvance}");
    }
    #endregion
}