using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/SubmarineElement")]
public class SubmarineMapElementSO : WorldMapUIElementSO
{
    [SerializeField] private GameObject submarinePrefab;
    
    public override GameObject CreateElement()
    {
        GameObject go = Instantiate(submarinePrefab);
        
        WorldMapUIElement component = go.AddComponent<WorldMapUIElement>();
        component.Setup(updateMode, syncMode, mapAsset, syncTime);
        
        return go;
    }
}
