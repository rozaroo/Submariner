using UnityEngine;
using UnityEngine.InputSystem;

public class PeriscopeTrigger : MonoBehaviour
{
    public GameObject playerCamera;
    public GameObject periscopeCamera;
    public PlayerController playerController;
    public PeriscopeController periscopeController;
    public InputActionReference interactAction;
    bool playerInside = false;
    bool usingPeriscope = false;

    void OnEnable()
    {
        interactAction.action.Enable();
    }
    void OnDisable()
    {
        interactAction.action.Disable();
    }
    void Update() 
    {
        if (playerInside && interactAction.action.triggered) TogglePeriscope();
    }
    void TogglePeriscope() 
    {
        usingPeriscope = !usingPeriscope;
        playerCamera.SetActive(!usingPeriscope);
        periscopeCamera.SetActive(usingPeriscope);
        playerController.enabled = !usingPeriscope;
        periscopeController.ActivatePeriscope(usingPeriscope);
        Cursor.lockState = usingPeriscope ? CursorLockMode.None : CursorLockMode.Locked;
    }
    void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player")) playerInside = true;
    }
    void OnTriggerExit(Collider other) 
    {
        if (other.CompareTag("Player")) 
        {
            playerInside = false;
            if (usingPeriscope) TogglePeriscope();
        }
    }
}
