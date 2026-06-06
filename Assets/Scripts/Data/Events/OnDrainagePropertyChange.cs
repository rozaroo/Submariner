using System;

[Serializable]
public struct OnDrainagePropertyChange : IGameEvent
{
    public float drainagePercentage;
    public OnDrainagePropertyChange(float dPercentage)
    {
        drainagePercentage = dPercentage;
    }
}