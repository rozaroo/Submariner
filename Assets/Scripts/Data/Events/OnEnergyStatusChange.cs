using System;

[Serializable]
public struct OnEnergyStatusChange : IGameEvent
{
    public EnergyStatus energyStatus;
}
