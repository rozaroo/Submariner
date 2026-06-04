using UnityEngine;

public class CameraForceLookState : IState, ITransferable
{
    private float _duration;
    private float _elapsedTime;
    
    private Vector3 _originalLocalPosition;
    private Vector3 _startLerpPosition;
    private Vector3 _targetPosition;
    private Vector3 _targetLookPosition;
    
    private float _originalYaw;
    private float _originalPitch;
    private float _startYaw;
    private float _startPitch;
    private float _targetYaw;
    private float _targetPitch;
    
    private bool _isReturning;
    private CameraContext _context;
    
    public IState nextState { get; private set; }
    public bool isComplete { get; private set; }

    public CameraForceLookState(CameraContext context, bool complete, IState nState)
    {
        _context = context;
        isComplete = complete;
        nextState = nState;
    }
    
    public void SetupState(Vector3 targetMovePosition, Vector3 targetLookPosition, float duration, IState nState, bool isReturning = false)
    {
        _targetPosition = targetMovePosition;
        _targetLookPosition = targetLookPosition;
        _duration = duration;
        nextState = nState;
        _isReturning = isReturning;
        isComplete = false;
    }

    public void OnEnter()
{
    _elapsedTime = 0f;
    
    if (!_isReturning) 
    {
        _originalYaw = _context.CameraRotationData.CurrentYaw; //Storage
        _originalPitch = _context.CameraRotationData.CurrentPitch; //Storage
        _originalLocalPosition = _context.CameraTransform.localPosition; //Storage 
        
        Log.Info($"{_originalYaw}, {_originalPitch}, {_originalLocalPosition}");
        
        _startLerpPosition = _context.CameraTransform.position;
        
        _startYaw = _context.CameraRotationData.CurrentYaw;
        _startPitch = _context.CameraRotationData.CurrentPitch;
        
        Vector3 direction = _targetLookPosition - _targetPosition;
        if (direction.sqrMagnitude > 0.001f)
        {
            _targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
            _targetPitch = -Mathf.Atan2(direction.y, flatDirection.magnitude) * Mathf.Rad2Deg;
        }
    }
    else 
    {
        _startLerpPosition = _context.CameraTransform.localPosition;
        
        _startYaw = _context.CameraTransform.eulerAngles.y;
        _startPitch = _context.CameraTransform.eulerAngles.x;
        
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
        
        if (normalizedTime >= 1f)
        {
            OnTransitionComplete();
        }
        else
        { 
            CameraMovement(t);
            CameraRotation(t);
        }
    }

    public void OnExit()
    {
        if (!_isReturning)
        {
            _context.CameraTransform.position = _targetPosition;

            _context.BodyTransform.rotation = Quaternion.Euler(0f, _targetYaw, 0f);
            _context.CameraTransform.localRotation = Quaternion.Euler(_targetPitch, 0f, 0f);
        }
        else
        {
            _context.CameraTransform.localPosition = _originalLocalPosition;
            _context.CameraTransform.localRotation = Quaternion.Euler(_targetPitch, 0f, 0f);
        }
        StoreData(_targetYaw, _targetPitch);
    }
    
    private void CameraMovement(float t)
    {
        if (!_isReturning)
        {
            _context.CameraTransform.position = Vector3.Lerp(_startLerpPosition, _targetPosition, t);
        }
        else
        {
            _context.CameraTransform.localPosition = Vector3.Lerp(_startLerpPosition, _originalLocalPosition, t);
        }
    }

    private void CameraRotation(float t)
    {
        float currentLerpYaw   = Mathf.LerpAngle(_startYaw,   _targetYaw,   t);
        float currentLerpPitch = Mathf.LerpAngle(_startPitch, _targetPitch, t);
        
        _context.BodyTransform.rotation      = Quaternion.Euler(0f, currentLerpYaw, 0f);
        _context.CameraTransform.localRotation = Quaternion.Euler(currentLerpPitch, 0f, 0f);
    }
    
    private void OnTransitionComplete()
    {
        isComplete = true;
    }
    
    private void StoreData(float newYaw, float newPitch)
    {
        _context.CameraRotationData.Yaw = newYaw;
        _context.CameraRotationData.Pitch = newPitch;
        _context.CameraRotationData.CurrentYaw = newYaw;
        _context.CameraRotationData.CurrentPitch = newPitch;
    }
}