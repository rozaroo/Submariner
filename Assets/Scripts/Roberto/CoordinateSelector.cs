using UnityEngine;

public class CoordinateSelector : MonoBehaviour
{
    public Submarine submarine;
    int selectedIndex = 0;
    
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
}
