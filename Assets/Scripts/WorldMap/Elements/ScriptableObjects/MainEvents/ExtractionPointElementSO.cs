using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/ExtractionPoint")]
public class ExtractionPointElementSO : WorldMapUIElementSO
{
    protected override void ConfigureElement(GameObject go)
    {
        if (!go.TryGetComponent(out ExtractionPointElement _))
            Log.Warning($"[ExtractionPointElementSO] Prefab '{go.name}' doesnt have ExtractionPointElement.");
    }
}