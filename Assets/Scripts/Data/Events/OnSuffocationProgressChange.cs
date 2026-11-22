public struct OnSuffocationProgressChange : IGameEvent
{
    public float currentSuffocationProgress;
    public float secondsRemaining;

    public OnSuffocationProgressChange(float currentSuffocationProgress, float secondsRemaining)
    {
        this.currentSuffocationProgress = currentSuffocationProgress;
        this.secondsRemaining = secondsRemaining;
    }
}
