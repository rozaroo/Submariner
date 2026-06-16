using System;
using UnityEngine;

public class WorldMapElement : MonoBehaviour, IWorldElement, ISetup
{
    public bool IsInitialized { get; private set; }
    public Vector3 position => transform.position;
    public Vector3 rotation => transform.rotation.eulerAngles;
    public SonarDetectionMode sonarDetectionMode { get; private set; } = SonarDetectionMode.None;

    public event Action<IWorldElement> OnElementDestroyed;

    private void OnDestroy() => OnElementDestroyed?.Invoke(this);
    
    protected void ForceReleaseFromUI()
    {
        OnElementDestroyed?.Invoke(this);
    }

    public void Setup()
    {
        if (IsInitialized) return;
        Setup(SonarDetectionMode.None);
    }
    
    public void Setup(SonarDetectionMode sDetectionMode)
    {
        if (IsInitialized) return;
        sonarDetectionMode = sDetectionMode;
        IsInitialized = true;
    }

    #if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        float radius = 1f;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(position, radius);
    }
    #endif
}
