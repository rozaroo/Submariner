using UnityEngine;

public class ButtonInteractionAdapter : MonoBehaviour, IInteractable
{
    private IButtonControls _buttonControls;

    private void Awake()
    {
        _buttonControls = GetComponent<IButtonControls>();
        if (_buttonControls == null)
        {
            Log.Error($"{nameof(ButtonInteractionAdapter)} requires IButtonControls Component available for: {gameObject.name}.");
        }
    }

    public void Interact(PlayerCharacter player)
    {
        _buttonControls?.OnActionDown();
    }
}