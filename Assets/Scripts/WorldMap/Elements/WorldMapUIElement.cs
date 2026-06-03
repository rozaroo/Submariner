using UnityEngine;

public class WorldMapUIElement : WorldMapElement, IWorldMapUIElement
{
    private MapAssetSO _mapAsset;
    private WorldUIUpdateMode _updateMode = WorldUIUpdateMode.Static;
    private WorldUISyncMode _syncMode = WorldUISyncMode.Linear;
    private float _syncTime;

    public MapAssetSO mapAsset => _mapAsset;
    public WorldUIUpdateMode updateMode => _updateMode;
    public WorldUISyncMode syncMode => _syncMode;
    public float syncTime => _syncTime;
    
    public void Setup(SonarDetectionMode sMode, MapAssetSO assSo, WorldUIUpdateMode uMode, WorldUISyncMode sModeUI, float sTime = 0.1f)
    {
        if (IsInitialized) return;
        
        base.Setup(sMode);

        _mapAsset = assSo;
        _updateMode = uMode;
        _syncMode = sModeUI;
        _syncTime = sTime;
    }
}