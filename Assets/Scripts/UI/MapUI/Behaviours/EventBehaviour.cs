using UnityEngine;
public class EventBehaviour : MonoBehaviour, ISetup
{
    public WorldEventBehaviourSO worldEvent { get; private set; }
    public bool IsInitialized { get; private set; }

    public void Setup() => Setup(worldEvent);

    public void Setup(WorldEventBehaviourSO desiredEvent)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        worldEvent = desiredEvent;
    }
}