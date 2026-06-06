using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public static class MapIconFactory
{
    private static readonly Dictionary<MapAssetSO, ObjectPool<MapIcon>> _pools = new();
    public static MapIcon Create(MapAssetSO config, RectTransform parent)
    {
        if (!_pools.ContainsKey(config))
        {
            _pools[config] = new ObjectPool<MapIcon>(
                createFunc: () => CreateNewIcon(config, parent),
                
                actionOnGet: icon => 
                {
                    icon.IsVisible = true;
                },
                actionOnRelease: icon => 
                {
                    icon.ResetToDefaultState();
                    icon.IsVisible = false;
                },
                
                actionOnDestroy: icon => Object.Destroy(icon.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }
        MapIcon spawnedIcon = _pools[config].Get();
        spawnedIcon.transform.SetParent(parent, false); 
        return spawnedIcon;
    }
    
    private static MapIcon CreateNewIcon(MapAssetSO config, RectTransform parent)
    {
        var go = new GameObject(config.assetName);
        go.transform.SetParent(parent, false);

        var icon = go.AddComponent<MapIcon>();
        icon.MapAssetConfig = config;
        icon.Setup();

        return icon;
    }
    
    public static void Release(MapIcon icon)
    {
        if (icon == null || icon.MapAssetConfig == null) return;

        if (_pools.TryGetValue(icon.MapAssetConfig, out ObjectPool<MapIcon> pool))
        {
            pool.Release(icon);
        }
        else
        {
            Object.Destroy(icon.gameObject); 
        }
    }
}