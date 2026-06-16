using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/MainWorldEvent")]
public class MainWorldEventSO : WorldMapUIElementSO
{
    [Header("Mission Narrative")]
    [SerializeField] private string missionName = "New Mission";
    [TextArea(2, 4)]
    [SerializeField] private string objectiveDescription = "Mission Details...";

    protected override void ConfigureElement(GameObject go)
    {
        if (!go.TryGetComponent(out MainWorldEvent mainEvent))
        {
            Log.Warning($"[MainWorldEventSO] Prefab '{go.name}' doesnt have MainWorldEvent.");
            return;
        }
        
        mainEvent.InjectMissionData(missionName, objectiveDescription);
        
        mainEvent.Setup(DetectionMode, MapAsset, UpdateMode, SyncMode, SyncTime);
    }
}