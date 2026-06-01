using System;
using UnityEngine;

public interface IWorldMapUIElement
{
    Vector3 Position { get; }
    Vector3 Rotation { get; }
    MapAssetSO MapAsset { get; }
    WorldUIUpdateMode UpdateMode { get; }
    WorldUISyncMode SyncMode { get; }
    float SyncTime { get; }
    event Action<IWorldMapUIElement> OnElementDestroyed;
}