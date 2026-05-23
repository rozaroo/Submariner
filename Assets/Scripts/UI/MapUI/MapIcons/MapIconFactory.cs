using UnityEngine;
using UnityEngine.UI;

public static class MapIconFactory
{
    public static MapIcon Create(MapAssetSO config, RectTransform parent)
    {
        var go = new GameObject(config.name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>();

        var icon = go.AddComponent<MapIcon>();
        icon.MapAssetConfig = config;
        icon.Setup();

        return icon;
    }
}