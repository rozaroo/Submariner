using UnityEngine;
using UnityEngine.InputSystem;

public class NavegationStation : MonoBehaviour
{
    //CoordinateSwitcher
    public InputActionReference switchAction;

    //CoordinateSelector
    public Submarine submarine;
    int selectedIndex = 0;

    //LeverController
    public InputActionReference interactAction;
    bool playerInside = false;

    void OnEnable()
    {
        switchAction.action.Enable();
        interactAction.action.Enable();
    }
    void OnDisable()
    {
        switchAction.action.Disable();
        interactAction.action.Disable();
    }

    void Update()
    {
        if (switchAction.action.triggered) SelectNext();
        if (playerInside && interactAction.action.triggered) ConfirmSelection();
    }
    public void SelectNext()
    {
        selectedIndex++;
        if (selectedIndex >= submarine.destinations.Length) selectedIndex = 0;
        Debug.Log("Selected destination: " + selectedIndex);
    }
    public void SelectDestination(int index)
    {
        if (index < 0 || index >= submarine.destinations.Length) return;
        selectedIndex = index;
        Debug.Log("Destino seleccionado: " + selectedIndex);
    }
    public void ConfirmSelection()
    {
        submarine.StartTravel(selectedIndex);
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
