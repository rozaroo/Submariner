
using UnityEngine;

public class WorldElementSOTest : WorldMapUIElementSO
{
    public override GameObject CreateElement()
    {
        GameObject go = new GameObject();
        go.AddComponent<WorldMapUIElement>();
        return go;
    }
}
