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
        ICameraRotation cam = _context.CameraRotationData;
        Vector2 lookDir = Vector2.zero;
        
        if (_context.LookAction != null && _context.LookAction.enabled)
        {
            lookDir = _context.LookAction.ReadValue<Vector2>();
        }
        
        CameraMovement(cam, lookDir);
    }

    public void OnExit() { }
    
    private void CameraMovement(ICameraRotation rData, Vector2 lookDir)
    {
        rData.Yaw += lookDir.x * _context.LookSensitivity * Time.deltaTime;
        rData.Pitch -= lookDir.y * _context.LookSensitivity * Time.deltaTime;
        rData.Pitch = Mathf.Clamp(rData.Pitch, -_context.UpDownLookLimit, _context.UpDownLookLimit);
        
        if (_context.LookLerpSpeed <= 50)
        {
            float t = 1f - Mathf.Exp(-_context.LookLerpSpeed * Time.deltaTime);
            rData.CurrentYaw = Mathf.LerpAngle(rData.CurrentYaw, rData.Yaw, t);
            rData.CurrentPitch = Mathf.LerpAngle(rData.CurrentPitch, rData.Pitch, t);
        }
        else
        {
            rData.CurrentYaw = rData.Yaw;
            rData.CurrentPitch = rData.Pitch;
        }
        
        _context.BodyTransform.rotation = Quaternion.Euler(0f, rData.CurrentYaw, 0f);
        _context.CameraTransform.localRotation = Quaternion.Euler(rData.CurrentPitch, 0f, 0f);
    }
}