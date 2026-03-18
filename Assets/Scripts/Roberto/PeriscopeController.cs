using UnityEngine;
using UnityEngine.InputSystem;

public class PeriscopeController : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public Transform periscopeHead;
    public InputActionReference moveAction;
    bool isActive = false;

    void OnEnable() 
    {
        moveAction.action.Enable();
    }
    void OnDisable()
    {
        moveAction.action.Disable();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created<>
    public void ActivatePeriscope(bool state) 
    {
        isActive = state;
    }
    void Update() 
    {
        if (!isActive) return;
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        float turn = input.x; //A / D
        periscopeHead.Rotate(Vector3.up * turn * rotationSpeed * Time.deltaTime);
    }
}
