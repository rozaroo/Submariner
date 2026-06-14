using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Elements/CollisionElement")]
public class WorldMapCollisionElementSO : WorldMapElementSO
{
    [Header("Scale Variation")]
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.5f, 2f);
    
    protected override void ConfigureElement(GameObject go)
    {
        float scaleFactor = Random.Range(scaleRange.x, scaleRange.y);
        go.transform.localScale = baseScale * scaleFactor;

        if (!go.TryGetComponent<WorldMapCollisionElement>(out var element))
        {
            element = go.AddComponent<WorldMapCollisionElement>();
        }

        go.layer = LayerMask.NameToLayer("ExternalCollision");
        
        element.Setup(DetectionMode);
    }
}