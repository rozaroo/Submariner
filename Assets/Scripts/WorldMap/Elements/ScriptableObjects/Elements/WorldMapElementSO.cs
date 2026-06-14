using UnityEngine;

public abstract class WorldMapElementSO : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] protected string elementName;
    [SerializeField] protected GameObject prefab;
    [SerializeField] protected float requiredSize;

    [Header("Sonar Logic Settings")]
    [SerializeField] protected SonarDetectionMode SonarDetectionMode;

    public float RequiredSize => requiredSize;
    protected SonarDetectionMode DetectionMode => SonarDetectionMode;
    
    public GameObject CreateElement()
    {
        GameObject go = prefab != null ? Instantiate(prefab) : new GameObject();

        go.name = elementName;
        ConfigureElement(go);
        return go;
    }
    
    protected abstract void ConfigureElement(GameObject go);
}