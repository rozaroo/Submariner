using System;

public static class GameEventChannel<T> where T : struct, IGameEvent
{
    public static event Action<T> OnEventRaised;

    public static void RaiseEvent(T value)
    {
        OnEventRaised?.Invoke(value);
    }
}