using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    private float movSmoothing;
    private float rotSmoothing;
    void Start()
    {
        movSmoothing = 0.04;
        rotSmoothing = 0.02;
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, target.position, movSmoothing);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotSmoothing);
    }
}
