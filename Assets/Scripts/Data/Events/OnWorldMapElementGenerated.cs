public struct OnWorldMapElementGenerated : IGameEvent
{
    public IWorldMapUIElement _worldElementGenerated;
    
    public OnWorldMapElementGenerated(IWorldMapUIElement worldElementGenerated)
    {
        _worldElementGenerated = worldElementGenerated;
    }
}
