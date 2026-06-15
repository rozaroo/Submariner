using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public static class MapIconFactory
{
    private static readonly ObjectPoolFactory<MapAssetSO, MapIcon> _pool =
        new(config => new ObjectPool<MapIcon>(
            createFunc:      () => CreateNewIcon(config),
            actionOnGet:     icon => icon.IsVisible = true,
            actionOnRelease: icon => { icon.ResetToDefaultState(); icon.IsVisible = false; },
            actionOnDestroy: icon => Object.Destroy(icon.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize:         50
        ));

    public static MapIcon Create(MapAssetSO config, RectTransform parent)
    {
        MapIcon icon = _pool.Get(config);
        icon.transform.SetParent(parent, false);
        return icon;
    }

    public static void Release(MapIcon icon)
    {
        if (icon == null || icon.MapAssetConfig == null) return;
        _pool.Release(icon.MapAssetConfig, icon);
    }

    private static MapIcon CreateNewIcon(MapAssetSO config)
    {
        var go = new GameObject(config.assetName);
        var icon = go.AddComponent<MapIcon>();
        icon.MapAssetConfig = config;
        icon.Setup();
        return icon;
    }
}