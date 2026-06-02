using UnityEngine;

public abstract class WorldMapUIElementSO : WorldMapElementSO
{
    [Header("UI Sincronization Properties")]
    [SerializeField] protected WorldUIUpdateMode updateMode;
    [SerializeField] protected WorldUISyncMode syncMode;
    [SerializeField] protected float syncTime = 0.1f;

    public float SyncTime => syncTime;
    public WorldUIUpdateMode UpdateMode => updateMode;
    public WorldUISyncMode SyncMode => syncMode;
}