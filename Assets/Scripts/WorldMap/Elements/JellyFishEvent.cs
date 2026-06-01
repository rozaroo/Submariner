using System;
using UnityEngine;

public class JellyFishEvent : MonoBehaviour, IEvent
{
    public bool IsActive { get; set; } //TODO: Implement to Condition   
    public void Execute()
    {
        Log.Info("[JellyFishEvent] Executing JellyFish Event");
    }

    public bool CheckConditions()
    {
        Log.Info("[JellyFishEvent] CheckingConditions JellyFish Event");
        return true; //TODO: Implement actual conditions for the event to be active
    }

    public void EndEvent()
    {
        Log.Info("[JellyFishEvent] CheckingConditions JellyFish Event");
    }
}
