using UnityEngine;

public abstract class WorldMapElementSO : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] protected string elementName;
    [SerializeField] protected float requiredSize;
    
    [Header("Sonar Logic Settings")]
    [SerializeField] protected SonarDetectionMode sonarDetectionMode;
    
    public float RequiredSize => requiredSize;
    public SonarDetectionMode SonarDetectionMode => sonarDetectionMode;

    public abstract GameObject CreateElement();
}