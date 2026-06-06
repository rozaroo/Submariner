using System;

[Serializable]
public struct OnWorldSubmarineGenerated : IGameEvent
{
    public IWorldMapUIElement _submarineElement;

    public OnWorldSubmarineGenerated(IWorldMapUIElement submarineElement)
    {
        _submarineElement = submarineElement;
    }
}
