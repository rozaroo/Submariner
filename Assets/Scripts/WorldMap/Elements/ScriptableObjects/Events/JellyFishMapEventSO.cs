using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Events/JellyFishEvent")]
public class JellyFishMapEventSO : WorldMapElementSO
{
    [Header("Flocking Spawner Settings")]
    [SerializeField] private GameObject flockAgentPrefab;

    [Header("Flocking Physics")]
    [SerializeField] private FlockingSettingsSO flockingSettings;

    public override GameObject CreateElement()
    {
        GameObject go = new GameObject(elementName);
        
        WorldMapElement mapElementComp = go.AddComponent<WorldMapElement>();
        mapElementComp.Setup(updateMode, syncMode, mapAsset, syncTime);
        
        FlockingCore flockCore = go.AddComponent<FlockingCore>();
        flockCore.Setup(flockingSettings, flockAgentPrefab);
        flockCore.enabled = false; 
        
        JellyFishEvent jellyFishEvent = go.AddComponent<JellyFishEvent>();
        
        jellyFishEvent.InjectFlockingEngine(flockCore);
        
        return go;
    }
}