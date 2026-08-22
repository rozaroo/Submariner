using UnityEngine;

public class LeverInteractionAdapter : MonoBehaviour, IInteractable
{
    private LeverInteractionStation _interactionStation;

    private void Awake()
    {
        _interactionStation = GetComponent<LeverInteractionStation>();
        if (_interactionStation == null) Log.Error($"{name}: Missing LeverInteractionStation.");
    }

    public void Interact(PlayerCharacter player)
    {
        _interactionStation.Possess(player);
    }
}