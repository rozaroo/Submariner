using System;

[Serializable]
public struct OnOxygenChanged : IGameEvent
{
    public float currentOxygen;
    public float maxOxygen;
    public OnOxygenChanged(float currentOxygen, float maxOxygen)
    {
        this.currentOxygen = currentOxygen;
        this.maxOxygen = maxOxygen;
    }
}