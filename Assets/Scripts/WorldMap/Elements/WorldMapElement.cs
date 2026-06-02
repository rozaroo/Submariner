using System;
using UnityEngine;

public class WorldMapElement : MonoBehaviour, IWorldMapUIElement, ISetup
{
    [SerializeField] private WorldUIUpdateMode updateMode = WorldUIUpdateMode.Static;
    [SerializeField] private WorldUISyncMode syncMode = WorldUISyncMode.Linear;
    public MapAssetSO MapAsset { get; private set;}
    public bool IsInitialized { get; private set; }
    public Vector3 Position => transform.position;
    public Vector3 Rotation => transform.rotation.eulerAngles;
    public SonarDetectionMode SonarDetectionMode => 
        MapAsset != null ? MapAsset.sonarInteractionRule : SonarDetectionMode.Both;
    public event Action<IWorldElement> OnElementDestroyed;

    public WorldUIUpdateMode UpdateMode
    {
        get => updateMode;
        private set => updateMode = value;
    }
    
    public WorldUISyncMode SyncMode 
    {
        get => syncMode;
        private set => syncMode = value;
    }
    
    public float SyncTime { get; private set; }
    
    private void OnDestroy() => OnElementDestroyed?.Invoke(this);
    public void Setup() => Setup(UpdateMode, SyncMode, MapAsset);

    public void Setup(WorldUIUpdateMode uMode, WorldUISyncMode sMode, 
        MapAssetSO mapAsset, float syncTime = 0.1f)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        UpdateMode = uMode;
        SyncMode = sMode;
        SyncTime = syncTime;
        MapAsset = mapAsset;
    }
    
    #if UNITY_EDITOR
    public void OnDrawGizmos()// Debugging Only
    {
        float radius = 1f; 
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Position, radius);
    }
    #endif
}
