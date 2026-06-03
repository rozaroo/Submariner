public interface IWorldMapUIElement : IWorldElement 
{
    MapAssetSO mapAsset { get; }
    WorldUIUpdateMode updateMode { get; }
    WorldUISyncMode syncMode { get; }
    float syncTime { get; }
}