using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/SubmarineElement")]
public class SubmarineMapElementSO : WorldMapElementSO
{
    [SerializeField] private GameObject submarinePrefab;
    public override GameObject CreateElement()
    {
        GameObject go = Instantiate(submarinePrefab);
        WorldMapElement component = go.AddComponent<WorldMapElement>();
        component.Setup(updateMode, syncMode, mapAsset);
        return go;
    }
}
