public interface IWorldMapUIElement : IWorldElement 
{
    MapAssetSO MapAsset { get; }
    WorldUIUpdateMode UpdateMode { get; }
    WorldUISyncMode SyncMode { get; }
    float SyncTime { get; }
}