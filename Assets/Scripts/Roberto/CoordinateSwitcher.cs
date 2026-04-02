using UnityEngine;
using UnityEngine.InputSystem;

public class CoordinateSwitcher : MonoBehaviour
{
    public CoordinateSelector selector;
    public InputActionReference switchAction;
    void OnEnable() => switchAction.action.Enable();
    void OnDisable() => switchAction.action.Disable();

    void Update()
    {
        if (switchAction.action.triggered) selector.SelectNext();
    }
}
