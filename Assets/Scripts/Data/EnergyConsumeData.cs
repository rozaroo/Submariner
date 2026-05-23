using System;

[Serializable]
public struct EnergyConsumeData
{
    public float energyToConsumeRate; //Use ONLY positive values. The system will determine whether to add or relieve stress based on the isAddingStress boolean.
    public bool isAddingStress; //Whether the energy change is adding stress to the system (true) or relieving it (false).
}