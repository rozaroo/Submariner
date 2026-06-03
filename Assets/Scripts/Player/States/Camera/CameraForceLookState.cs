using UnityEngine;

public class CameraForceLookState : IState
{
    private CameraContext _context;
    
    private float _duration;
    private float _elapsedTime;
    
    private Vector3 _originalLocalPosition;
    private Vector3 _originalPosition;
    private Vector3 _targetPosition;
    
    private Vector3 _targetLookPosition;
    private float _originalYaw;
    private float _originalPitch;
    private float _startYaw;
    private float _startPitch;
    private float _targetYaw;
    private float _targetPitch;
    
    private bool _isReturning;

    public CameraForceLookState(CameraContext context, StateMachine stateMachine)
    {
        _context = context;
    }
    
    public void SetupTransition(Vector3 targetMovePosition, Vector3 targetLookPosition, float duration, bool isReturning = false)
    {
        _targetPosition = targetMovePosition;
        _targetLookPosition = targetLookPosition;
        _duration = duration;
        _isReturning = isReturning;
    }

    public void OnEnter()
{
    _elapsedTime = 0f;
    
    if (!_isReturning) 
    {
        _originalYaw = _context.CameraRotation.CurrentYaw;
        _originalPitch = _context.CameraRotation.CurrentPitch;
        _originalLocalPosition = _context.CameraTransform.localPosition;
        _originalPosition = _context.CameraTransform.position;
        
        _startYaw = _context.CameraRotation.CurrentYaw;
        _startPitch = _context.CameraRotation.CurrentPitch;
        
        Vector3 direction = _targetLookPosition - _context.BodyTransform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            _targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            
            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
            _targetPitch = -Mathf.Atan2(direction.y, flatDirection.magnitude) * Mathf.Rad2Deg;
        }
    }
    else 
    {
        _originalPosition = _context.CameraTransform.localPosition;
        _startYaw = _context.CameraRotation.CurrentYaw;
        _startPitch = _context.CameraRotation.CurrentPitch;
        
        _targetYaw = _originalYaw;
        _targetPitch = _originalPitch;
    }
}

    public void Update() { }

    public void LateUpdate()
    {
        _elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);
        float t = Mathf.SmoothStep(0f, 1f, normalizedTime);
        
        CameraMovement(t);
        CameraRotation(t);
        
        if (normalizedTime >= 1f)
        {
            OnTransitionComplete();
        }
    }

    public void OnExit()
    {
        if (_isReturning)
        {
            _context.CameraTransform.localPosition = _originalLocalPosition;
            ForceRotationInstant(_targetYaw, _targetPitch);
        }
        else
        {
            _context.CameraTransform.position = _targetPosition;
            ForceRotationInstant(_targetYaw, _targetPitch);
        }
    }

    private void OnTransitionComplete()
    {
        if (!_isReturning)
        {
            Log.Info("Camera To Desired Position.");
        }
        else
        {
            Log.Info("Back To Original Position.");
        }
    }

    private void CameraMovement(float t)
    {
        if (!_isReturning)
        {
            _context.CameraTransform.position = Vector3.Lerp(_originalPosition, _targetPosition, t);
        }
        else
        {
            _context.CameraTransform.localPosition = Vector3.Lerp(_originalPosition, _originalLocalPosition, t);
        }
    }

    private void CameraRotation(float t)
    {
        _context.CameraRotation.CurrentYaw = Mathf.LerpAngle(_startYaw, _targetYaw, t);
        _context.CameraRotation.CurrentPitch = Mathf.LerpAngle(_startPitch, _targetPitch, t);
 
        _context.BodyTransform.rotation = Quaternion.Euler(0f, _context.CameraRotation.CurrentYaw, 0f);
        _context.CameraTransform.localRotation = Quaternion.Euler(_context.CameraRotation.CurrentPitch, 0f, 0f);
    }
    
    private void ForceRotationInstant(float newYaw, float newPitch)
    {
        _context.CameraRotation.Yaw = newYaw;
        _context.CameraRotation.Pitch = newPitch;
        _context.CameraRotation.CurrentYaw = newYaw;
        _context.CameraRotation.CurrentPitch = newPitch;
    }
}