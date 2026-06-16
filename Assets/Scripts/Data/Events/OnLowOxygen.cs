public struct OnLowOxygen : IGameEvent
{
    public bool IsLow;

    public OnLowOxygen(bool isLow)
    {
        IsLow = isLow;
    }
}
