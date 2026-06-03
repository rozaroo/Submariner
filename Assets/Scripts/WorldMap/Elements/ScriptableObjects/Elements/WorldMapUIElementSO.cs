using UnityEngine;

public abstract class WorldMapUIElementSO : WorldMapElementSO
{
    [Header("UI Synchronization Properties")]
    [SerializeField] protected MapAssetSO mapAsset;
    [SerializeField] protected WorldUIUpdateMode updateMode;
    [SerializeField] protected WorldUISyncMode syncMode;
    [SerializeField] protected float syncTime = 0.1f;
    
    protected MapAssetSO MapAsset => mapAsset;
    protected float SyncTime => syncTime;
    protected WorldUIUpdateMode UpdateMode => updateMode;
    protected WorldUISyncMode SyncMode => syncMode;
}