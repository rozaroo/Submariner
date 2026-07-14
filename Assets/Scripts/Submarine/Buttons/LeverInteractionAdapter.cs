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
        if (_leverControls == null || _leverControls.isLocked) return;
        _leverControls.SetActive(!_leverControls.isActive);
    }
}