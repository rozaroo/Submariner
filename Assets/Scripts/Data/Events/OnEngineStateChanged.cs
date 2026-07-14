using System;

[Serializable]
public struct OnEngineStateChanged : IGameEvent
{
    public EngineState State;
    public float SpeedMultiplier;
}