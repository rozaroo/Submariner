public struct OnPlayerInputStateChanged : IGameEvent
{
    public readonly bool IsInputEnabled;

    public OnPlayerInputStateChanged(bool isInputEnabled)
    {
        IsInputEnabled = isInputEnabled;
    }
}