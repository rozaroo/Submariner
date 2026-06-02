using UnityEngine;

[CreateAssetMenu(fileName = "FlockingSettings", menuName = "Flocking/Settings")]
public class FlockingSettingsSO : ScriptableObject
{
    [Header("Spawn Settings")]
    public int spawnAmount = 10;
    public float spawnRadius = 20f;
    public float containmentRadius = 30f;
    
    [Header("Speed Limits")]
    public float minSpeed = 2f;
    public float maxSpeed = 5f;

    [Header("Radar Radius")]
    public float neighborRadius = 4f;
    public float avoidanceRadius = 1.5f;

    [Header("Behavior Weights")]
    [Range(0f, 5f)] public float cohesionWeight = 1.0f;
    [Range(0f, 5f)] public float alignmentWeight = 1.0f;
    [Range(0f, 5f)] public float avoidanceWeight = 1.5f;
    [Range(0f, 5f)] public float boundsWeight = 2.0f;

    [Header("Movement Smoothing")]
    [Range(0.1f, 10f)] public float rotationSpeed = 5f;
}