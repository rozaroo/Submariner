using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    { 
        Move();
        Turn();
        Rise();
        Stabilize();
    }
    void Move() 
    {
        if (Input.GetKey(KeyCode.W)) curSpeed += speedChangeAmount;
        else if (Input.GetKey(KeyCode.S)) curSpeed -= speedChangeAmount;
        else if (Mathf.Abs(curSpeed) <= minSpeed) curSpeed = 0;
        curSpeed = Mathf.Clamp(curSpeed, -maxBackwardSpeed, maxForwardSpeed);
        rb.AddForce(transform.forward * curSpeed);
    }
    void Turn() 
    {
        if (Input.GetKey(KeyCode.D)) rb.AddTorque(transform.up * turnSpeed);
        else if (Input.GetKey(KeyCode.A)) rb.AddTorque(transform.up * -turnSpeed);
    }
    void Rise() 
    {
        if (Input.GetKey(KeyCode.LeftShift)) rb.AddForce(transform.up * riseSpeed);
        else if (Input.GetKey(KeyCode.LeftControl)) rb.AddForce(transform.up * -riseSpeed);
    }
    void Stabilize() 
    {
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(new Vector3(0, rb.rotation.eulerAngles.y, 0)), stabilizationSmoothing));
    }
}
