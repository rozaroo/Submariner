
public class WorldMapUIElement : WorldMapElement, IWorldMapUIElement
{
    private WorldUIUpdateMode _updateMode = WorldUIUpdateMode.Static;
    private WorldUISyncMode _syncMode = WorldUISyncMode.Linear;
    private float _syncTime;

    public WorldUIUpdateMode UpdateMode => _updateMode;
    public WorldUISyncMode SyncMode => _syncMode;
    public float SyncTime => _syncTime;
    
    public void Setup(WorldUIUpdateMode uMode, WorldUISyncMode sMode, MapAssetSO mapAsset, float syncTime = 0.1f)
    {
        if (IsInitialized) return;
        
        base.Setup(mapAsset);

        _updateMode = uMode;
        _syncMode = sMode;
        _syncTime = syncTime;
    }
}