public struct OnInteraction : IGameEvent
{
    public InteractionFeedback InteractionFeedback;
    
    public OnInteraction(InteractionFeedback interactionFeedback)
    {
        InteractionFeedback = interactionFeedback;
    }
}
