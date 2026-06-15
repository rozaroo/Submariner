using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainEventManager : MonoBehaviour
{
    [Header("Main Events (orden de progresión, 4-5 recomendados)")]
    [SerializeField] private List<MainWorldEvent> mainEvents = new List<MainWorldEvent>();

    [Header("Extraction Point")]
    [SerializeField] private ExtractionPointElement extractionPoint;
    [SerializeField] private MapAssetSO extractionMapAsset;

    [Header("Radar Indicator")]
    [SerializeField] private float mainEventIndicatorRadius = 15f;

    [Header("Pacing")]
    [SerializeField] private float delayBetweenEvents = 1.5f;

    private int _currentIndex;

    private void Awake()
    {
        if (extractionPoint != null)
            extractionPoint.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised += OnRadarStateChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised -= OnRadarStateChanged;
    }

    private void Start()
    {
        if (mainEvents.Count > 0)
            BroadcastCurrentArea();
        else
            Log.Warning("[MainEventManager] No hay Main Events configurados.");
    }

    private void OnRadarStateChanged(OnSonarElementsDetection property)
    {
        if (!property.IsRevealed) return;
        if (property.SonarRegion != SonarDetectionMode.InnerOnly) return;
        if (_currentIndex >= mainEvents.Count) return;

        if (!ReferenceEquals(property.WorldElement, mainEvents[_currentIndex])) return;

        IMainWorldEvent current = mainEvents[_currentIndex];
        if (!current.CheckConditions()) return;

        current.Execute();
        StartCoroutine(AdvanceAfterDelay());
    }

    private IEnumerator AdvanceAfterDelay()
    {
        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(Vector3.zero, 0f, false));

        yield return new WaitForSeconds(delayBetweenEvents);

        _currentIndex++;
        if (_currentIndex >= mainEvents.Count)
            UnlockExtractionPoint();
        else
            BroadcastCurrentArea();
    }

    private void BroadcastCurrentArea()
    {
        IWorldElement worldElement = mainEvents[_currentIndex];
        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(worldElement.position, mainEventIndicatorRadius, true));
    }

    private void UnlockExtractionPoint()
    {
        if (extractionPoint == null)
        {
            Log.Warning("[MainEventManager] Extraction Point Not Assigned.");
            return;
        }

        extractionPoint.Setup(SonarDetectionMode.Both, extractionMapAsset, WorldUIUpdateMode.Static, WorldUISyncMode.Linear);
        extractionPoint.gameObject.SetActive(true);
        extractionPoint.OnSubmarineReachedExtraction += OnSubmarineReachedExtraction;

        GameEventChannel<OnWorldMapElementGenerated>.RaiseEvent(new OnWorldMapElementGenerated(extractionPoint));

        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(extractionPoint.position, mainEventIndicatorRadius, true));

        GameEventChannel<OnExtractionPointUnlocked>.RaiseEvent(new OnExtractionPointUnlocked());
    }

    private void OnSubmarineReachedExtraction()
    {
        extractionPoint.OnSubmarineReachedExtraction -= OnSubmarineReachedExtraction;

        GameEventChannel<OnMainEventAreaChanged>.RaiseEvent(
            new OnMainEventAreaChanged(Vector3.zero, 0f, false));

        GameEventChannel<OnGameWon>.RaiseEvent(new OnGameWon());
    }

    #region Testing
    [ContextMenu("Force Advance Main Event")]
    private void DebugAdvance()
    {
        if (_currentIndex < mainEvents.Count)
        {
            mainEvents[_currentIndex].Execute();
            StartCoroutine(AdvanceAfterDelay());
        }
    }

    [ContextMenu("Check State")]
    private void DebugState()
    {
        Log.Info($"[MainEventManager] Actual Event: {_currentIndex}/{mainEvents.Count}");
    }
    #endregion
}