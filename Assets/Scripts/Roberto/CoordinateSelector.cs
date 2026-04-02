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
    public void ConfirmSelection() 
    {
        submarine.StartTravel(selectedIndex);
    }
}
