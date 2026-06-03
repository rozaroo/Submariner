using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Events/JellyFishEvent")]
public class JellyFishMapEventSO : WorldMapUIElementSO
{
    [Header("Flocking Spawner Settings")]
    [SerializeField] private GameObject flockAgentPrefab;

    [Header("Flocking Physics")]
    [SerializeField] private FlockingSettingsSO flockingSettings;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private bool canMove = false;
    
    public override GameObject CreateElement()
    {
        GameObject go = new GameObject(elementName);
        
        FlockingCore flockCore = go.AddComponent<FlockingCore>();
        flockCore.Setup(flockingSettings, flockAgentPrefab);
        flockCore.enabled = false; 
        
        JellyFishEvent jellyFishEvent = go.AddComponent<JellyFishEvent>();
        
        jellyFishEvent.Setup(this.SonarDetectionMode, MapAsset, UpdateMode, SyncMode, SyncTime);
        jellyFishEvent.InjectFlockingEngine(flockCore, patrolSpeed, canMove);
        
        return go;
    }
}