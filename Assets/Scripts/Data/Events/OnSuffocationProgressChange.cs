using UnityEngine;

public struct OnSuffocationProgressChange : IGameEvent
{
    public float currentSuffocationProgress;
    
    public OnSuffocationProgressChange(float currentSuffocationProgress)
    {
        this.currentSuffocationProgress = currentSuffocationProgress;
    }
}
