using UnityEngine;

public abstract class WorldMapElementSO : ScriptableObject
{
    [SerializeField] protected string elementName;
    [SerializeField] protected float requiredSize;
    [SerializeField] protected WorldUIUpdateMode updateMode;
    [SerializeField] protected WorldUISyncMode syncMode;
    [SerializeField] protected MapAssetSO mapAsset;
    [SerializeField] protected float syncTime = 0.1f;
    
    public float SyncTime => syncTime;
    public float RequiredSize => requiredSize;
    public abstract GameObject CreateElement();
}
