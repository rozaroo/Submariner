public struct OnSonarElementsDetection : IGameEvent
{
    public IWorldElement WorldElement;
    public SonarDetectionMode SonarRegion;
    public bool IsRevealed;
    
    public OnSonarElementsDetection(IWorldElement worldElement,SonarDetectionMode sonarRegion, bool isRevealed)
    {
        WorldElement = worldElement;
        SonarRegion = sonarRegion;
        IsRevealed = isRevealed;
    }
}


