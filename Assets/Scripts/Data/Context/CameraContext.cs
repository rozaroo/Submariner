using UnityEngine;
using UnityEngine.InputSystem;

public class CameraContext
{
    public Transform PlayerTransform;
    public Transform CameraTransform;
    public InputAction LookAction;
    
    public float Yaw;
    public float Pitch;
    public float CurrentYaw;
    public float CurrentPitch;
    
    public float LookSensitivity;
    public float UpDownPitchLimit;
    public float LookLerpSpeed;
}