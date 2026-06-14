using UnityEngine;
 
public class DazedCameraStrategy : ICameraStrategy
{
    private readonly float _sensitivityMultiplier;
    private readonly float _lerpSpeedOverride;
    private readonly float _maxRollAngle = 30f;
    private readonly float _pronePitch;
    private readonly Vector3 _impactDir;
    
    private Vector3 _standingLocalPosition;
    private Vector3 _proneLocalPosition;
    private Vector3 _positionOnGetUp;
    
    private float _frozenBodyYaw;
    
    private float _standingPitch;
    private float _targetLocalPitch;
    private float _currentLocalPitch;
    private float _targetLocalYaw;
    private float _currentLocalYaw;
    private float _targetLocalRoll;
    private float _currentLocalRoll;
    private float _pitchOnGetUp;
    
    
    private enum Phase { Falling, OnFloor, GettingUp, Done }
    private Phase _phase = Phase.Falling;
 
    private float _fallingProgress;
    private float _gettingUpProgress;
 
    private readonly AnimationCurve _getUpCurve;
    public bool IsFinished => _phase == Phase.Done;
    
    public DazedCameraStrategy(AnimationCurve getUpCurve, Vector3 impactDir, float sensitivityMultiplier = 0.4f, float lerpSpeedOverride = 3f, float pronePitch = 80f)
    {
        _getUpCurve = getUpCurve;
        _sensitivityMultiplier = sensitivityMultiplier;
        _lerpSpeedOverride = lerpSpeedOverride;
        _pronePitch = pronePitch;
        _impactDir = impactDir;
    }
    
    public void SetFallingProgress(float t) => _fallingProgress = Mathf.Clamp01(t); //Set
    public void SetGettingUpProgress(float t) => _gettingUpProgress = Mathf.Clamp01(t); //Set
 
    public void StartOnFloor()
    {
        if (_phase != Phase.Falling) return;
        _phase = Phase.OnFloor;
    }
 
    public void StartGettingUp()
    {
        if (_phase != Phase.OnFloor) return;
        _pitchOnGetUp    = _currentLocalPitch;
        _positionOnGetUp = _phase == Phase.OnFloor ? _proneLocalPosition : _standingLocalPosition;
        _positionOnGetUp = _proneLocalPosition;
        _phase = Phase.GettingUp;
    }

    #region CameraStrategy

    public void Enter(CameraContext ctx)
    {
        _frozenBodyYaw = ctx.SmoothedYaw;
 
        _standingPitch = ctx.SmoothedPitch;
        _targetLocalPitch = ctx.SmoothedPitch;
        _currentLocalPitch = ctx.SmoothedPitch;
        _targetLocalYaw = 0f;
        _currentLocalYaw = 0f;
        
        float rightDot = Vector3.Dot(_impactDir, ctx.CameraTransform.right);
        _targetLocalRoll  = -rightDot * _maxRollAngle;
        _currentLocalRoll = 0f;
        
        _standingLocalPosition = ctx.CameraTransform.localPosition;
        float proneY = -_standingLocalPosition.y + 0.05f;
        _proneLocalPosition = new Vector3(_standingLocalPosition.x, proneY, _standingLocalPosition.z);
 
        _phase = Phase.Falling;
        _fallingProgress = 0f;
        _gettingUpProgress = 0f;
    }
 
    public void Look(CameraContext ctx)
    {
        float smooth = 1f - Mathf.Exp(-_lerpSpeedOverride * Time.deltaTime);
 
        switch (_phase)
        {
            case Phase.Falling:   
                UpdateFalling(ctx, smooth);   
                break;
            
            case Phase.OnFloor:
                UpdateOnFloor(ctx, smooth);
                break;
            
            case Phase.GettingUp: 
                UpdateGettingUp(ctx, smooth); 
                break;
        }
 
        ctx.CameraTransform.localRotation = Quaternion.Euler(_currentLocalPitch, _currentLocalYaw, _currentLocalRoll);
    }
 
    public void Exit(CameraContext ctx)
    {
        ctx.CameraTransform.localPosition = _standingLocalPosition;
 
        ctx.InputYaw    = _frozenBodyYaw + _targetLocalYaw;
        ctx.SmoothedYaw = _frozenBodyYaw + _currentLocalYaw;
 
        ctx.InputPitch    = _targetLocalPitch;
        ctx.SmoothedPitch = _currentLocalPitch;
 
        ctx.PlayerTransform.rotation =
            Quaternion.Euler(0f, ctx.SmoothedYaw, 0f);
        ctx.CameraTransform.localRotation =
            Quaternion.Euler(ctx.SmoothedPitch, 0f, 0f);
    }

    #endregion

    #region Updates

    private void UpdateFalling(CameraContext ctx, float smooth)
    {
        float eased = EaseOutQuad(_fallingProgress);
        
        ctx.CameraTransform.localPosition = Vector3.Lerp(_standingLocalPosition, _proneLocalPosition, eased);
        
        _targetLocalPitch  = Mathf.Lerp(_standingPitch, _pronePitch, eased);
        _currentLocalPitch = Mathf.LerpAngle(_currentLocalPitch, _targetLocalPitch, smooth);
        _currentLocalYaw   = Mathf.LerpAngle(_currentLocalYaw, _targetLocalYaw, smooth * 0.3f);
        
        float rollPeak   = Mathf.Sin(_fallingProgress * Mathf.PI); // 0→1→0
        float rollSettle = Mathf.Lerp(0f, _targetLocalRoll * 0.4f, eased); // Settle
        float rollTarget = Mathf.Lerp(_targetLocalRoll * rollPeak, rollSettle, eased);
        _currentLocalRoll = Mathf.LerpAngle(_currentLocalRoll, rollTarget, smooth);
    }
 
    private void UpdateOnFloor(CameraContext ctx, float smooth)
    {
        ctx.CameraTransform.localPosition = _proneLocalPosition;
        
        Vector2 lookDir = ctx.LookAction.ReadValue<Vector2>();
        _targetLocalYaw += lookDir.x * (ctx.LookSensitivity * _sensitivityMultiplier) * Time.deltaTime;
        _targetLocalPitch -= lookDir.y * (ctx.LookSensitivity * _sensitivityMultiplier) * Time.deltaTime;
 
        _targetLocalPitch = Mathf.Clamp(_targetLocalPitch, _pronePitch - 15f, _pronePitch + 10f);
        _targetLocalYaw = Mathf.Clamp(_targetLocalYaw, -70f, 70f);
 
        _currentLocalPitch = Mathf.LerpAngle(_currentLocalPitch, _targetLocalPitch, smooth);
        _currentLocalYaw = Mathf.LerpAngle(_currentLocalYaw,   _targetLocalYaw,   smooth);
        
        _currentLocalRoll = Mathf.LerpAngle(_currentLocalRoll, 0f, smooth * 0.4f);
    }
 
    private void UpdateGettingUp(CameraContext ctx, float smooth)
    {
        float curveValue = _getUpCurve.Evaluate(_gettingUpProgress);
        
        ctx.CameraTransform.localPosition = Vector3.Lerp(_proneLocalPosition, _standingLocalPosition, Mathf.Clamp01(curveValue));
        
        _targetLocalPitch = Mathf.Lerp(_pitchOnGetUp, _standingPitch, curveValue);
        _currentLocalPitch = Mathf.LerpAngle(_currentLocalPitch, _targetLocalPitch, smooth);
        _currentLocalYaw = Mathf.LerpAngle(_currentLocalYaw,   _targetLocalYaw,   smooth * 0.5f);
        
        _currentLocalRoll = Mathf.LerpAngle(_currentLocalRoll, 0f, smooth * 0.8f);
 
        if (_gettingUpProgress >= 1f) 
            _phase = Phase.Done;
    }

    #endregion
 
    private static float EaseOutQuad(float t) => t * (2f - t); //Type of Curve.
}