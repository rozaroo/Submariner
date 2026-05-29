using UnityEngine;

public class EventBehaviour : MonoBehaviour, ISetup
{
    private WorldEventSO _worldEvent;
    public bool IsInitialized { get; private set; }

    public void Setup() => Setup(_worldEvent);

    public void Setup(WorldEventSO desiredEvent)  //TODO: Maybe this is Useless. Only Useful to detect if Event has been Stored.
    {
        if (IsInitialized) return;
        IsInitialized = true;
        _worldEvent = desiredEvent;
    }
}
