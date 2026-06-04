using UnityEngine;
using UnityEngine.InputSystem;

public class CameraContext
{
    public ICameraRotation CameraRotationData { get; private set; }
    public Transform CameraTransform { get; private set; }
    public Transform BodyTransform { get; private set; }
    public float LookSensitivity { get; private set; }
    public float UpDownLookLimit { get; private set; }
    public float LookLerpSpeed { get; private set; }
    public InputAction LookAction { get; private set; }

    public CameraContext(ICameraRotation cameraRotationData, Transform cameraTransform, Transform bodyTransform, float lookSensitivity, 
        float upDownLookLimit, float lookLerpSpeed, InputAction lookAction)
    {
        CameraRotationData = cameraRotationData;
        CameraTransform = cameraTransform;
        BodyTransform = bodyTransform;
        LookSensitivity = lookSensitivity;
        UpDownLookLimit = upDownLookLimit;
        LookLerpSpeed = lookLerpSpeed;
        LookAction = lookAction;
    }
}