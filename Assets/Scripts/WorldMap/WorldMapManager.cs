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
    [SerializeField] private float minDistanceBetweenEvents = 5f; //Minimal Limit
    [SerializeField] private WorldMapElementSO submarineSo;
    [SerializeField] private List<WorldMapElementSO> worldPossibleElements = new List<WorldMapElementSO>(); 
    
    //TODO: Maybe transfer "Map Properties" to a ScriptableObject if we want to have
    //different map generation settings, or even make it so the map generation settings are defined by the MapAssetSO of each element,
    //so we can have different generation settings for each type of element.
    
    private Dictionary<GameObject, float> _mapElements = new Dictionary<GameObject, float>();
    private GameObject _submarineGo;
    
    private void Start()
    {
        GenerateMap();
        CreateSubmarine();
    }

    #region Map Generation

    [ContextMenu("Create Map")]
    public void GenerateMap() //NOTE: This method is used only at the start of the game, if you want to generate individual elements use the GenerateIndividualElement method.
    {
        int removedElements = 0;
        int elementsNumber = 0;
        Dictionary<GameObject,float> generatedElements = new Dictionary<GameObject,float>();

        for (int i = 0; i < desiredEventsAmount; i++)
        {
            WorldMapElementSO mapElementSo = SelectWorldElement();
            GameObject elementGo = GenerateWorldElementGo(mapElementSo);
            if (elementGo != null)
            {
                elementGo.transform.SetParent(worldContainerGo.transform, false);
                generatedElements.Add(elementGo, mapElementSo.RequiredSize);
            }
            else
            {
                Log.Warning("[WorldMapManager] No world events available to generate.");
                break;
            }
        }

        foreach (var pair in generatedElements)
        {
            if (TryAssignPosition(pair.Key.transform, pair.Value))
            {
                elementsNumber++;
                _mapElements.Add(pair.Key, pair.Value);
                Log.Info($"Event Number: {elementsNumber} - Spawning at {pair.Key.transform.position}");
            }
            else
            {
                Destroy(pair.Key);
                removedElements++;
            }
        }

        OnWorldMapGeneratedProperty onWorldMapProperties =
            new OnWorldMapGeneratedProperty(ValidateListForUI(_mapElements));
        GameEventChannel<OnWorldMapGeneratedProperty>.RaiseEvent(onWorldMapProperties);
        Log.Info($"Removed {removedElements} events. Total placed: {_mapElements.Count}.");
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
    }
    
    private GameObject GenerateIndividualElement(WorldMapElementSO mapElementSo)
    {
        GameObject elementGo = GenerateWorldElementGo(mapElementSo);
        if (elementGo != null)
        {
            elementGo.transform.SetParent(worldContainerGo.transform, false);
            if (TryAssignPosition(elementGo.transform, mapElementSo.RequiredSize))
            {
                _mapElements.Add(elementGo, mapElementSo.RequiredSize);
                Log.Info($"Spawning at {elementGo.transform.position}");
                
                IWorldMapUIElement uiElement = CheckForWorldMapUIElement(elementGo);
                if (uiElement != null)
                    GameEventChannel<OnWorldMapElementGenerated>.RaiseEvent(new  OnWorldMapElementGenerated(uiElement));

                return elementGo;
            }
            Destroy(elementGo);
        }
        return null;
    }
    
    private GameObject GenerateWorldElementGo(WorldMapElementSO mapElementSo)
    {
        if (mapElementSo != null)
        {
            GameObject elementGo = mapElementSo.CreateElement();
            return elementGo;
        }
        return null;
    }
    
    private WorldMapElementSO SelectWorldElement()
    {
        if (worldPossibleElements.Count > 0)
            return worldPossibleElements[Random.Range(0, worldPossibleElements.Count)];
        return null;
    }

    #endregion
    
    #region Positioning Tools

    private bool TryAssignPosition(Transform goTransform, float requiredSize = 0f)
    {
        if (_mapElements.Count > 0)
        {
            for (int i = 0; i <= mapSpawnAttempts; i++)
            {
                Vector3 desiredPosition = GenerateRandomPosition();
                if (TrySetPosition(desiredPosition, requiredSize))
                {
                    goTransform.position = desiredPosition;
                    return true;
                }
            }
            return false;
        }
        goTransform.position = GenerateRandomPosition();
        return true;
    }
    
    private bool TrySetPosition(Vector3 desiredPosition, float requiredSize)
    {
        foreach (var pair in _mapElements)
        {
            if (CheckForOverlap(desiredPosition, pair.Key.transform.position, requiredSize, pair.Value))
                return false;
        }
        return true;
    }
    
    private bool CheckForOverlap(Vector3 desired, Vector3 existing, float sizeA, float sizeB)
    {
        float minRequiredDistance = sizeA + sizeB + minDistanceBetweenEvents;
        float minRequiredDistanceSqr = minRequiredDistance * minRequiredDistance; 
    
        return (desired - existing).sqrMagnitude < minRequiredDistanceSqr;
    }

    private Vector3 GenerateRandomPosition()
    {
        float halfSize = mapRuntimeData.worldMapSize / 2f;
        float x = Random.Range(-halfSize, halfSize);
        float z = Random.Range(-halfSize, halfSize);
        return worldContainerGo.transform.position + new Vector3(x, 0, z);
    }
    
    #endregion

    #region UI Tools

    private List<IWorldMapUIElement> ValidateListForUI(Dictionary<GameObject,float> mapElements)
    {
        List<IWorldMapUIElement> validatedUIElements = new List<IWorldMapUIElement>();
        foreach (var pair in mapElements)
        {
            IWorldMapUIElement uiElement = CheckForWorldMapUIElement(pair.Key);
            if (uiElement != null)
                validatedUIElements.Add(uiElement);
        }
        return validatedUIElements;
    }
    
    private IWorldMapUIElement CheckForWorldMapUIElement(GameObject go)
    {
        IWorldMapUIElement uiElement = go.GetComponent<IWorldMapUIElement>();
        return uiElement;
    }

    #endregion
    
    private void CreateSubmarine()
    {
        if(_submarineGo != null)
        {
            _submarineGo.transform.position = GenerateRandomPosition();
            _submarineGo.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            _mapElements.Add(_submarineGo, 0);
            IWorldMapUIElement uiElement = CheckForWorldMapUIElement(_submarineGo);
            if (uiElement != null)
                GameEventChannel<OnWorldSubmarineGenerated>.RaiseEvent(new OnWorldSubmarineGenerated(uiElement));
            else
            {
                Log.Warning("[WorldMapManager] Could not find a valid position.");
            }
        }
        else if (submarineSo != null)
        {
            _submarineGo = GenerateIndividualElement(submarineSo);
            if (_submarineGo == null)
            {
                Log.Warning("[WorldMapManager] Could not find a valid position for the submarine.");
                return;
            }
            IWorldMapUIElement uiElement = CheckForWorldMapUIElement(_submarineGo);
            if (uiElement != null)
                GameEventChannel<OnWorldSubmarineGenerated>.RaiseEvent(new OnWorldSubmarineGenerated(uiElement));
        }
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (worldContainerGo != null && mapRuntimeData != null)
        {
            Gizmos.DrawWireCube(worldContainerGo.transform.position, new Vector3(mapRuntimeData.worldMapSize, 0, mapRuntimeData.worldMapSize));   
        }
    }
    
    [ContextMenu("Check Map Elements Count")]
    private void CheckValues()
    {
        Log.Info($"{_mapElements.Count}");
    }
    
    #endif
}