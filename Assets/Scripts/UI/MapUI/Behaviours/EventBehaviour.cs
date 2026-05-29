using UnityEngine;

public class EventBehaviour : MonoBehaviour, ISetup     //TEMPORAL, Change to event with type of icon to filter
{
    public bool IsInitialized { get; private set; }

    public void Setup()
    {
        if (IsInitialized) return;
        IsInitialized = true;
    }
}
