using UnityEngine;
using UnityEngine.InputSystem;

public class LeverController : MonoBehaviour
{
    public CoordinateSelector selector;
    public InputActionReference interactAction;
    bool playerInside = false;
    void OnEnable() => interactAction.action.Enable();
    void OnDisable() => interactAction.action.Disable();

    void Update()
    {
        if (playerInside && interactAction.action.triggered) selector.ConfirmSelection();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }
}
