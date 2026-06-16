using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class WorldMapElementSO : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] protected string elementName;
    [SerializeField] protected GameObject prefab;

    [Header("Sonar Logic Settings")]
    [SerializeField] protected SonarDetectionMode SonarDetectionMode;

    [Header("Size")]
    [SerializeField] private float requiredSize;
    [SerializeField] private float sizeMultiplier = 1f;
    
    [Header("Spawn Weight")]
    [SerializeField] private float spawnWeight = 1f;
    
    public float SpawnWeight => spawnWeight;
    public float RequiredSize => requiredSize;
    public float LastRequiredSize { get; private set; }

    protected SonarDetectionMode DetectionMode => SonarDetectionMode;

    public GameObject CreateElement()
    {
        LastRequiredSize = requiredSize;

        GameObject go = prefab != null ? Instantiate(prefab) : new GameObject();
        go.name = elementName;
        ConfigureElement(go);
        return go;
    }
    
    protected void SetLastRequiredSize(float scaledSize)
    {
        LastRequiredSize = scaledSize;
    }

    protected abstract void ConfigureElement(GameObject go);

#if UNITY_EDITOR
    [ContextMenu("Calculate From Prefab")]
    private void CalculateRequiredSizeFromPrefab()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[{name}] No Prefab Assigned.");
            return;
        }

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[{name}] Prefab doesnt contain Renderer. RequiredSize not modified.");
            DestroyImmediate(temp);
            return;
        }

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        DestroyImmediate(temp);

        float calculatedRadius = combined.extents.magnitude * sizeMultiplier;
        Undo.RecordObject(this, "Calculate RequiredSize");
        requiredSize = calculatedRadius;
        EditorUtility.SetDirty(this);

        Debug.Log($"[{name}] RequiredSize calculated: {requiredSize:F2} (Multiplier: {sizeMultiplier})");
    }
#endif
}