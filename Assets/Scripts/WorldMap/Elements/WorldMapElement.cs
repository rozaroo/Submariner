using System;
using UnityEngine;

public class WorldMapElement : MonoBehaviour, IWorldElement, ISetup
{
    public MapAssetSO MapAsset { get; private set; }
    public bool IsInitialized { get; private set; }
    public Vector3 Position => transform.position;
    public Vector3 Rotation => transform.rotation.eulerAngles;

    public SonarDetectionMode SonarDetectionMode =>
        MapAsset != null ? MapAsset.sonarInteractionRule : SonarDetectionMode.Both;

    public event Action<IWorldElement> OnElementDestroyed;

    private void OnDestroy() => OnElementDestroyed?.Invoke(this);

    public void Setup() => Setup(MapAsset);

    public void Setup(MapAssetSO mapAsset)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        MapAsset = mapAsset;
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        float radius = 1f;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Position, radius);
    }
#endif
}

