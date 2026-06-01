using UnityEngine;

[CreateAssetMenu(menuName = "WorldMap/Events/JellyFishEvent")]
public class JellyFishMapEventSo : WorldMapEventSo
{
    public override GameObject CreateElement()
    {
        GameObject go = new GameObject(elementName);
        WorldMapElement component = go.AddComponent<WorldMapElement>();
        component.Setup(updateMode, syncMode, mapAsset);
        JellyFishEvent jellyFishEvent = go.AddComponent<JellyFishEvent>();
        ApplyEvent(jellyFishEvent);
        return go;
    }
    public override void ApplyEvent(IEvent go)
    {
        Log.Info("[JellyFishEventSO] Applying JellyFish Event");
    }
}