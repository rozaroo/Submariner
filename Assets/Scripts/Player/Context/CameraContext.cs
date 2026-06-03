using UnityEngine;
using UnityEngine.InputSystem;

public class CameraContext
{
    public ICameraRotation CameraRotation { get; private set; }
    public Transform CameraTransform { get; private set; }
    public Transform BodyTransform { get; private set; }
    public float LookSensitivity { get; private set; }
    public float UpDownLookLimit { get; private set; }
    public float LookLerpSpeed { get; private set; }
    public InputAction LookAction { get; private set; }

    public CameraContext(ICameraRotation cameraRotation, Transform cameraTransform, Transform bodyTransform, float lookSensitivity, 
        float upDownLookLimit, float lookLerpSpeed, InputAction lookAction)
    {
        CameraRotation = cameraRotation;
        CameraTransform = cameraTransform;
        BodyTransform = bodyTransform;
        LookSensitivity = lookSensitivity;
        UpDownLookLimit = upDownLookLimit;
        LookLerpSpeed = lookLerpSpeed;
        LookAction = lookAction;
    }
}