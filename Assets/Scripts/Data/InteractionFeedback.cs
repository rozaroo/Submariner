using System;

[Serializable]
public struct InteractionFeedback
{
    public string messageToDisplay;
    public InteractionUIData interactionUIData;
    public bool canInteract;

    public InteractionFeedback(string messageToDisplay, InteractionUIData interactionUIData, bool canInteract = true)
    {
        this.messageToDisplay = messageToDisplay;
        this.interactionUIData = interactionUIData;
        this.canInteract = canInteract;
    }
}