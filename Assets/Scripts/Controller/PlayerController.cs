using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// //Temporal - Check Usage Later
    /// </summary>
    public IPossessable controlledObject;

    public void Possess(IPossessable other)
    {
        if (controlledObject != other)
        {
            if (controlledObject != null)
            {
                controlledObject.UnPossess();
            }
            controlledObject = other;
            controlledObject.Possess();
        }
    }

    public void UnPossess()
    {
        if (controlledObject != null)
        {
            controlledObject.UnPossess();
        }
    }
}
