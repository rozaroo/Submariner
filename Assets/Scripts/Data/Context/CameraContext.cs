using UnityEngine;
using UnityEngine.InputSystem;

public class CameraContext
{
    public Transform PlayerTransform;
    public Transform CameraTransform;
    public InputAction LookAction;
    
    public float InputYaw;
    public float InputPitch;
    public float InputRoll;
    public float SmoothedYaw;
    public float SmoothedPitch;
    public float SmoothedRoll;
    
    public float LookSensitivity;
    public float UpDownPitchLimit;
    public float LookLerpSpeed;
}