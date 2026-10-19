using UnityEngine;

public class LeverInteractionAdapter : MonoBehaviour, IInteractable
{
    private ILeverControls _leverControls;
    private LeverControlStation _station;

    private void Awake()
    {
        _leverControls = GetComponent<ILeverControls>(); 
        _station = GetComponentInParent<LeverControlStation>();
        if (_leverControls == null)
        {
            Log.Error($"{nameof(LeverInteractionAdapter)} requires ILeverControls Component available in {gameObject.name}.");
        }
    }

    public void Interact(PlayerCharacter player)
    {
        // A grouped lever station owns the interaction so E opens its panel
        // instead of immediately toggling the individual lever.
        
        if (_station != null)
        {
            _station.Interact(player);
            return;
        }

        Log.Info($"Lever Interaction -> {gameObject.name}");
        if (_leverControls == null || _leverControls.isLocked) return;
        _leverControls.SetActive(!_leverControls.isActive);
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        return _station.SendInteractionFeedback(player);
    }
}
