using UnityEngine;

public static class MapIconFactory
{
    public static MapIcon Create(MapAssetSO config, RectTransform parent)
    {
        var go = new GameObject(config.name);
        go.transform.SetParent(parent, false);

        var icon = go.AddComponent<MapIcon>();
        icon.MapAssetConfig = config;
        icon.Setup();

        return icon;
    }
}