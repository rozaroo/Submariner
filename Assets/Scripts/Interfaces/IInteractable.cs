public interface IInteractable
{
    void Interact(PlayerCharacter player);
    InteractionFeedback SendInteractionFeedback(PlayerCharacter player);
}
