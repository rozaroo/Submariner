using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 5f;
    public InputActionReference clickAction;
    void OnEnable() => clickAction.action.Enable();
    void OnDisable() => clickAction.action.Disable();

    // Update is called once per frame
    void Update()
    {
        if (clickAction.action.triggered)
        {
            Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                ConsoleButton button = hit.collider.GetComponent<ConsoleButton>();
                if (button != null) button.Press();
            }
        }
    }
}
