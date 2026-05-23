using UnityEngine;

public class ConsoleButton : MonoBehaviour
{
    public NavegationStation nav;
    public int destinationIndex;
    public void Press()
    {
        nav.SelectDestination(destinationIndex);
        Debug.Log("Botón presionado: " + destinationIndex);
    }
}
