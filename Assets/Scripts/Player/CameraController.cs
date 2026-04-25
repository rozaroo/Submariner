using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float _lookSensitivity = 100f;
    [SerializeField] private float _upDownLookLimit = 70f;
    [SerializeField] private float _lookLerpSpeed = 10f;
    
    [Header("Debug Settings")] 
    [SerializeField] private bool _showGizmos = true;
    
    [Header("References Settings")] 
    private PlayerInput _playerInput;
    [SerializeField] private Camera _camera;
    
    [Header("Rotation Settings")] 
    private float _pitch;
    private float _yaw;
    private float _currentPitch;
    private float _currentYaw;

    [Header("Coroutine Force Look Settings")] 
    private Coroutine _forceLookCoroutine;
    private bool _isForcedLooking;
    
    [Header("Coroutine Force Move Settings")] 
    private Coroutine _forceMoveCoroutine;
    private bool _isForcedMoving;
    
    private Vector3 _startingPosition;
    private Vector2 _lookDir;
    private InputAction _lookAction; // Referencia segura a la acción
    
    public Camera MainCamera { get {return _camera;} private set => _camera = value; }

    private void Awake()
    {
        _startingPosition = _camera.transform.localPosition;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _lookAction = _playerInput.actions.FindAction("Look");
    }
    private void Update()
    {
        if (_lookAction != null && _lookAction.enabled)
        {
            _lookDir = _lookAction.ReadValue<Vector2>();
        }
        else
        {
            _lookDir = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (!_isForcedLooking && !_isForcedMoving)
        {
            RotateCamera();
        }
    }
    
    private void RotateCamera()
    {
        _yaw += _lookDir.x * _lookSensitivity * Time.deltaTime;
        _pitch -= _lookDir.y * _lookSensitivity * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -_upDownLookLimit, _upDownLookLimit);

        float t = 1f - Mathf.Exp(-_lookLerpSpeed * Time.deltaTime);
        _currentYaw = Mathf.Lerp(_currentYaw, _yaw, t);
        _currentPitch = Mathf.Lerp(_currentPitch, _pitch, t);

        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
    }

    #region ForceMoveCamera

    public void ForceMoveCamera(Vector3 targetPosition, float duration = 1.0f)
    {
        if (_forceMoveCoroutine != null) StopCoroutine(_forceMoveCoroutine);
        _forceMoveCoroutine = StartCoroutine(MoveCameraToPositionRoutine(targetPosition, duration));
    }
    
    private IEnumerator MoveCameraToPositionRoutine(Vector3 targetPosition, float duration)
    {
        _isForcedMoving = true;

        Vector3 startPos = _camera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            _camera.transform.position = Vector3.Lerp(startPos, targetPosition, t);

            if (_showGizmos)
                Debug.DrawLine(_camera.transform.position, targetPosition, Color.cyan);

            yield return null;
        }
        
        _camera.transform.position = targetPosition;
        StopForceMoveCamera(false);
    }
    
    private void StopForceMoveCamera(bool returnToPosition)
    {
        if (_forceMoveCoroutine != null)
        {
            StopCoroutine(_forceMoveCoroutine);
            _forceMoveCoroutine = null;
        }

        if (returnToPosition)
            _camera.transform.localPosition = _startingPosition;
        
        _isForcedMoving = false;
    }
    
    public void ReturnToStartingPosition(float duration = 1.0f)
    {
        Vector3 globalReturnPos = transform.TransformPoint(_startingPosition);
        ForceMoveCamera(globalReturnPos, duration);
    }

    private void OnStopForceMoveCamera() => StopForceMoveCamera(true);

    #endregion

    #region RotateCameraInDirection

    public void ForceLookInDirection(Vector3 targetPosition, float duration = 1.0f)
    {
        if (_forceLookCoroutine != null) StopCoroutine(_forceLookCoroutine);
        _forceLookCoroutine = StartCoroutine(RotateCameraInDirectionRoutine(targetPosition, duration));
    }

    private void StopForceLook()
    {
        if (_forceLookCoroutine != null)
        {
            StopCoroutine(_forceLookCoroutine);
            _forceLookCoroutine = null;
        }
        _isForcedLooking = false;
    }
    
    public void ForceRotationInstant(float newYaw, float newPitch)
    {
        _yaw = newYaw;
        _currentYaw = newYaw;
        _pitch = newPitch;
        _currentPitch = newPitch;
    }
    
    private IEnumerator RotateCameraInDirectionRoutine(Vector3 targetPosition, float duration)
    {
        _isForcedLooking = true;
 
        if (_showGizmos)
            Debug.DrawRay(_camera.transform.position, targetPosition - _camera.transform.position, Color.aquamarine, 5f);
 
        float startYaw = _currentYaw;
        float startPitch = _currentPitch;

        Vector3 direction = targetPosition - _camera.transform.position;
        float targetYaw   = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float targetPitch = -Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;
 
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            _currentYaw   = Mathf.LerpAngle(startYaw, targetYaw, t);
            _currentPitch = Mathf.LerpAngle(startPitch, targetPitch, t);
 
            transform.rotation              = Quaternion.Euler(0f, _currentYaw, 0f);
            _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
 
            yield return null;
        }
        
        // Final Snap (Security)
        ForceRotationInstant(targetYaw, targetPitch);
        
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        
        StopForceLook();
    }

    #endregion
}