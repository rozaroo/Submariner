using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldMapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject worldContainerGo;

    [Header("Map Properties")]
    [SerializeField] private MapRuntimeDataSO mapRuntimeData;
    [SerializeField] private int desiredEventsAmount;
    [SerializeField] private int mapSpawnAttempts;
    [SerializeField] private float minDistanceBetweenEvents = 5f;
    
    [Header("Submarine")]
    [SerializeField] private WorldMapElementSO submarineSo;
    [SerializeField] private int submarineExtraAttempts = 50;

    [Header("Random Fill")]
    [SerializeField] private List<WorldMapElementSO> worldPossibleElements = new List<WorldMapElementSO>();

    [Header("Main Events (in Order)")]
    [SerializeField] private List<WorldMapElementSO> mainEventSOs = new List<WorldMapElementSO>();
    [SerializeField] private float mainEventExclusionRadius = 15f;

    [Header("Extraction Point")]
    [SerializeField] private WorldMapElementSO extractionPointSo;
    
    private readonly Dictionary<GameObject, float> _mapElements = new Dictionary<GameObject, float>();
    private readonly List<MainWorldEvent> _generatedMainEvents = new List<MainWorldEvent>();
    private GameObject _submarineGo;
    private ExtractionPointElement _generatedExtractionPoint;

    private void Start()
    {
        try
        {
            CreateSubmarine();
            GenerateMainEvents();
            GenerateExtractionPoint(); 
            GenerateRandomFill();
            GameEventChannel<OnMainEventsGenerated>.RaiseEvent(
                new OnMainEventsGenerated(_generatedMainEvents, _generatedExtractionPoint));
        
            GameEventChannel<OnWorldMapGeneratedProperty>.RaiseEvent(
                new OnWorldMapGeneratedProperty(ValidateListForUI(_mapElements)));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WorldMapManager] CRASH: {e.Message}\n{e.StackTrace}");
        }
        
    }

    #region Submarine

    private void CreateSubmarine()
    {
        if (submarineSo == null)
        {
            Log.Error("[WorldMapManager] Submarine SO not assigned.");
            return;
        }
        if (worldContainerGo == null)
        {
            Log.Error("[WorldMapManager] worldContainerGo is NULL. Check scene references.");
            return;
        }
        if (mapRuntimeData == null)
        {
            Log.Error("[WorldMapManager] mapRuntimeData is NULL. Check if SO is included in build.");
            return;
        }

        _submarineGo = GenerateWorldElementGo(submarineSo);

        if (_submarineGo == null)
        {
            Log.Error("[WorldMapManager] submarineSo.CreateElement() returned null. The prefab may be missing from the build.");
            return;
        }

        _submarineGo.transform.SetParent(worldContainerGo.transform, false);
        
        bool placed = TryAssignPosition(_submarineGo, submarineSo.LastRequiredSize, submarineExtraAttempts);

        if (!placed)
        {
            Log.Warning("[WorldMapManager] Submarine Position Not Found. Forcing to Zero.");
            Vector3 center = worldContainerGo.transform.position;
            ApplyPosition(_submarineGo, center);
        }

        _mapElements.Add(_submarineGo, submarineSo.LastRequiredSize);
        Log.Info($"[WorldMapManager] Submarine spawned at {_submarineGo.transform.position}");

        if (_submarineGo.TryGetComponent(out IWorldMapUIElement subUI))
            GameEventChannel<OnWorldSubmarineGenerated>.RaiseEvent(new OnWorldSubmarineGenerated(subUI));
    }

    #endregion

    #region Main Events

    private void GenerateMainEvents()
    {
        foreach (var so in mainEventSOs)
        {
            if (so == null) continue;

            GameObject go = GenerateWorldElementGo(so);
            if (go == null) continue;

            go.transform.SetParent(worldContainerGo.transform, false);
        
            if (!TryAssignPosition(go, mainEventExclusionRadius, mapSpawnAttempts))
            {
                Log.Warning($"[WorldMapManager] Main Event '{go.name}' location not found. Forcing position.");
                ApplyPosition(go, worldContainerGo.transform.position); 
            }
            //Turn Off For use in MainEventManager
            go.SetActive(false);

            _mapElements.Add(go, mainEventExclusionRadius);

            if (go.TryGetComponent(out MainWorldEvent mainEvent))
                _generatedMainEvents.Add(mainEvent);
        }
    }

    #endregion

    #region Extraction Point

    private void GenerateExtractionPoint()
    {
        if (extractionPointSo == null)
        {
            Log.Warning("[WorldMapManager] Extraction Point SO not assigned.");
            return;
        }

        GameObject go = GenerateWorldElementGo(extractionPointSo);
        if (go == null) return;

        go.transform.SetParent(worldContainerGo.transform, false);

        if (!TryAssignPosition(go, mainEventExclusionRadius, mapSpawnAttempts))
        {
            Log.Warning("[WorldMapManager] Extraction Point didnt found available location.");
            Destroy(go);
            return;
        }
        
        go.SetActive(false);

        _mapElements.Add(go, mainEventExclusionRadius);
        Log.Info($"[WorldMapManager] Extraction Point spawned at {go.transform.position}");

        if (go.TryGetComponent(out ExtractionPointElement extractionPoint))
            _generatedExtractionPoint = extractionPoint;
        else
            Log.Warning("[WorldMapManager] El Extraction Point doesnt have component ExtractionPointElement.");
    }

    #endregion

    #region Random Fill

    [ContextMenu("Create Map")]
    public void GenerateRandomFill()
    {
        int placed = 0;
        int removed = 0;
        
        var candidates = new Dictionary<GameObject, float>();

        for (int i = 0; i < desiredEventsAmount; i++)
        {
            WorldMapElementSO so = SelectRandomElement();
            if (so == null)
            {
                Log.Warning("[WorldMapManager] No elements available for random spawn.");
                break;
            }

            GameObject go = GenerateWorldElementGo(so);
            if (go != null)
            {
                go.transform.SetParent(worldContainerGo.transform, false);
                candidates.Add(go, so.LastRequiredSize);
            }
        }

        foreach (var pair in candidates)
        {
            if (TryAssignPosition(pair.Key, pair.Value, mapSpawnAttempts))
            {
                _mapElements.Add(pair.Key, pair.Value);
                placed++;
                Log.Info($"[WorldMapManager] Random Element Spawned at {pair.Key.transform.position}");
            }
            else
            {
                Destroy(pair.Key);
                removed++;
            }
        }

        Log.Info($"[WorldMapManager] Random Amount: {placed} placed, {removed} discarded.");
    }

    [ContextMenu("Reset Map")]
    public void ResetMap()
    {
        foreach (var pair in _mapElements)
        {
            if (pair.Key != null)
                Destroy(pair.Key);
        }
        _mapElements.Clear();
        _generatedMainEvents.Clear();
        _generatedExtractionPoint = null;
        _submarineGo = null;
    }

    private WorldMapElementSO SelectRandomElement()
    {
        if (worldPossibleElements.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var so in worldPossibleElements)
        {
            if (so != null) totalWeight += so.SpawnWeight;
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var so in worldPossibleElements)
        {
            if (so == null) continue;
            cumulative += so.SpawnWeight;
            if (roll < cumulative) return so;
        }

        return worldPossibleElements[^1]; //Last element if everything failed, TODO: maybe make a TemplateWorldElement for debugging?
    }

    private GameObject GenerateWorldElementGo(WorldMapElementSO so)
    {
        if (so == null) return null;
        try
        {
            return so.CreateElement();
        }
        catch (System.Exception e)
        {
            Log.Error($"[WorldMapManager] CreateElement() failed on SO '{so.name}': {e.Message}");
            return null;
        }
    }

    #endregion

    #region Positioning
    
    private bool TryAssignPosition(GameObject go, float requiredSize, int attempts)
    {
        if (_mapElements.Count == 0)
        {
            ApplyPosition(go, GenerateRandomPosition());
            return true;
        }

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = GenerateRandomPosition();
            if (IsPositionValid(candidate, requiredSize))
            {
                ApplyPosition(go, candidate);
                return true;
            }
        }
        return false;
    }

    private bool IsPositionValid(Vector3 candidate, float requiredSize)
    {
        foreach (var pair in _mapElements)
        {
            if (pair.Key == null) continue;
            if (CheckOverlap(candidate, pair.Key.transform.position, requiredSize, pair.Value))
                return false;
        }
        return true;
    }

    private bool CheckOverlap(Vector3 a, Vector3 b, float sizeA, float sizeB)
    {
        float minDist = sizeA + sizeB + minDistanceBetweenEvents;
        return (a - b).sqrMagnitude < minDist * minDist;
    }

    private void ApplyPosition(GameObject go, Vector3 position)
    {
        if (go.TryGetComponent(out Rigidbody rb))
        {
            var previousInterpolation = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position = position;
            rb.interpolation = previousInterpolation;
        }
        else
        {
            go.transform.position = position;
        }
    }

    private Vector3 GenerateRandomPosition()
    {
        if (mapRuntimeData == null)
        {
            Debug.LogError("[WorldMapManager] mapRuntimeData is NULL");
            return Vector3.zero;
        }
        if (worldContainerGo == null)
        {
            Debug.LogError("[WorldMapManager] worldContainerGo is NULL");
            return Vector3.zero;
        }
        float half = mapRuntimeData.worldMapSize / 2f;
        return worldContainerGo.transform.position + new Vector3(
            Random.Range(-half, half), 0f, Random.Range(-half, half));
    }

    #endregion

    #region UI Tools

    private List<IWorldMapUIElement> ValidateListForUI(Dictionary<GameObject, float> elements)
    {
        var result = new List<IWorldMapUIElement>();
        foreach (var pair in elements)
        {
            if (pair.Key == null) continue;
            if (!pair.Key.activeSelf) continue;
            if (pair.Key.TryGetComponent(out IWorldMapUIElement ui))
                result.Add(ui);
        }
        return result;
    }

    #endregion

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (worldContainerGo != null && mapRuntimeData != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(worldContainerGo.transform.position,
                new Vector3(mapRuntimeData.worldMapSize, 0, mapRuntimeData.worldMapSize));
        }

        Gizmos.color = new Color(0f, 1f, 0.3f, 0.5f);
        foreach (var ev in _generatedMainEvents)
        {
            if (ev == null) continue;
            Gizmos.DrawWireSphere(ev.position, mainEventExclusionRadius);
        }

        if (_generatedExtractionPoint != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.5f);
            Gizmos.DrawWireSphere(_generatedExtractionPoint.position, mainEventExclusionRadius);
        }
    }

    [ContextMenu("Check Map Elements Count")]
    private void CheckValues() => Log.Info($"[WorldMapManager] Elements on Map: {_mapElements.Count}");
    #endif
}