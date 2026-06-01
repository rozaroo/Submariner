public struct SonarElementsDetectionProperty
{
    public IWorldElement WorldElement;
    public bool IsRevealed;
    
    public SonarElementsDetectionProperty(IWorldElement worldElement, bool isRevealed)
    {
        WorldElement = worldElement;
        IsRevealed = isRevealed;
    }
}


