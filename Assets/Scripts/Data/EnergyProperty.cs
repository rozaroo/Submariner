using System;

[Serializable]
public struct EnergyProperty
{
    public float currentEnergyPercentage;
    public float maxEnergyPercentage; // just for reference, not really needed since it's always 100%
}