using UnityEngine;

public class CameraLookState : IState
{
    float currentVelocity; 
    float smoothTime = 0.1f;
    
    private CameraContext _context;
    public CameraLookState(CameraContext context)
    {
        _context = context;
    }

    public void OnEnter() { }
    public void Update() { }

    public void LateUpdate()
    {
        ICameraRotation cam = _context.CameraRotation;
        
        Vector2 lookDir = Vector2.zero;
        
        if (_context.LookAction != null && _context.LookAction.enabled)
        {
            lookDir = _context.LookAction.ReadValue<Vector2>();
        }
        
        cam.Yaw += lookDir.x * _context.LookSensitivity * Time.deltaTime;
        cam.Pitch -= lookDir.y * _context.LookSensitivity * Time.deltaTime;
        cam.Pitch = Mathf.Clamp(cam.Pitch, -_context.UpDownLookLimit, _context.UpDownLookLimit);
        
        if (_context.LookLerpSpeed <= 50)
        {
            float t = 1f - Mathf.Exp(-_context.LookLerpSpeed * Time.deltaTime);
            cam.CurrentYaw = Mathf.LerpAngle(cam.CurrentYaw, cam.Yaw, t);
            cam.CurrentPitch = Mathf.LerpAngle(cam.CurrentPitch, cam.Pitch, t);
        }
        else
        {
            cam.CurrentYaw = cam.Yaw;
            cam.CurrentPitch = cam.Pitch;
        }
        
        _context.BodyTransform.rotation = Quaternion.Euler(0f, cam.CurrentYaw, 0f);
        _context.CameraTransform.localRotation = Quaternion.Euler(cam.CurrentPitch, 0f, 0f);
    }

    public void OnExit() { }
}