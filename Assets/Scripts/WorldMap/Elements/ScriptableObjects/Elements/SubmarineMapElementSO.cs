using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/SubmarineElement")]
public class SubmarineMapElementSO : WorldMapUIElementSO
{
    protected override void ConfigureElement(GameObject go)
    {
        if (!go.TryGetComponent<WorldMapUIElement>(out var component))
        {
            component = go.AddComponent<WorldMapUIElement>();
        }

        component.Setup(DetectionMode, MapAsset, UpdateMode, SyncMode, SyncTime);
    }
}