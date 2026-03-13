using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SubController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created<<>>
    public float speedChangeAmount;
    public float maxForwardSpeed;
    public float maxBackwardSpeed;
    public float minSpeed;
    public float turnSpeed;
    public float curSpeed;
    public float riseSpeed;
    public float stabilizationSmoothing;
    public Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference riseAction;

    // Update is called once per frame
    void FixedUpdate()
    { 
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float riseInput = riseAction.action.ReadValue<float>();
        Move(moveInput.y);
        Turn(moveInput.x);
        Rise(riseInput);
        Stabilize();
    }
    void Move(float forwardInput) 
    {
        if (forwardInput > 0) curSpeed += speedChangeAmount;
        else if (forwardInput < 0) curSpeed -= speedChangeAmount;
        else if (Mathf.Abs(curSpeed) <= minSpeed) curSpeed = 0;
        curSpeed = Mathf.Clamp(curSpeed, -maxBackwardSpeed, maxForwardSpeed);
        rb.AddForce(transform.forward * curSpeed);
    }
    void Turn(float turnInput) 
    {
        rb.AddTorque(transform.up * turnInput * turnSpeed);
    }
    void Rise(float riseInput) 
    {
        rb.AddForce(transform.up * riseInput * riseSpeed);
    }
    void Stabilize() 
    {
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(new Vector3(0, rb.rotation.eulerAngles.y, 0)), stabilizationSmoothing));
    }
}
