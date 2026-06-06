using System;

[Serializable]
public struct OnEnergyPropertyChange : IGameEvent
{
    public float currentEnergyPercentage;
    public float maxEnergyPercentage; // just for reference, not really needed since it's always 100%
    
    public OnEnergyPropertyChange(float currentEnergyPercentage, float maxEnergyPercentage)
    {
        this.currentEnergyPercentage = currentEnergyPercentage;
        this.maxEnergyPercentage = maxEnergyPercentage;
    }
}