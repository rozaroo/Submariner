using System;

[Serializable]
public struct OnEnergyConsumption : IGameEvent
{
    public float energyToConsumeRate; //Use ONLY positive values. The system will determine whether to add or relieve stress based on the isAddingStress boolean.
    public bool isAddingStress; //Whether the energy change is adding stress to the system (true) or relieving it (false).

    public OnEnergyConsumption(float consumeRate, bool addingStress)
    {
        energyToConsumeRate = consumeRate;
        isAddingStress = addingStress;
    }
}