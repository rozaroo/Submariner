using System.Collections.Generic;

public struct OnWorldMapGeneratedProperty : IGameEvent
{
    public List<IWorldMapUIElement> MapElements;
    
    public OnWorldMapGeneratedProperty(List<IWorldMapUIElement> mapElements)
    {
        MapElements = mapElements;
    }
}
