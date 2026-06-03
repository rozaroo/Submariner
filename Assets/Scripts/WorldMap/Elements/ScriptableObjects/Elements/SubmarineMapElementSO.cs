using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/SubmarineElement")]
public class SubmarineMapElementSO : WorldMapUIElementSO
{
    [SerializeField] private GameObject submarinePrefab;
    
    public override GameObject CreateElement()
    {
        GameObject go = Instantiate(submarinePrefab);
        
        if (!go.TryGetComponent<WorldMapUIElement>(out var component))
        {
            component = go.AddComponent<WorldMapUIElement>();
        }
        
        component.Setup(this.SonarDetectionMode, MapAsset, UpdateMode, SyncMode, SyncTime);
        
        return go;
    }
}