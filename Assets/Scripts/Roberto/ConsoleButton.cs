using UnityEngine;

public class ConsoleButton : MonoBehaviour
{
    public NavegationSystem nav;
    public int destinationIndex;
    public void Press()
    {
        nav.SelectDestination(destinationIndex);
        Debug.Log("Botón presionado: " + destinationIndex);
    }
}
