using UnityEngine;

public class ConsoleButton : MonoBehaviour
{
    public CoordinateSelector selector;
    public int destinationIndex;
    public void Press()
    {
        selector.SelectDestination(destinationIndex);
        Debug.Log("Botón presionado: " + destinationIndex);
    }
}
