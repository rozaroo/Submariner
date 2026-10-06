using UnityEngine;

public class LeverInteractionAdapter : MonoBehaviour, IInteractable
{
    private ILeverControls _leverControls;

    private void Awake()
    {
        _leverControls = GetComponent<ILeverControls>();
        if (_leverControls == null)
        {
            Log.Error($"{nameof(LeverInteractionAdapter)} requires ILeverControls Component available in {gameObject.name}.");
        }
    }

    public void Interact(PlayerCharacter player)
    {
        // A grouped lever station owns the interaction so E opens its panel
        // instead of immediately toggling the individual lever.
        LeverControlStation station = GetComponentInParent<LeverControlStation>();
        if (station != null)
        {
            station.Interact(player);
            return;
        }

        Log.Info($"Lever Interaction -> {gameObject.name}");
        if (_leverControls == null || _leverControls.isLocked) return;
        _leverControls.SetActive(!_leverControls.isActive);
    }
}
