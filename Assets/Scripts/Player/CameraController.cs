using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float _lookSensitivity = 100f;
    [SerializeField] private float _upDownLookLimit = 70f;
    [SerializeField] private float _lookLerpSpeed = 10f;
    [SerializeField] private float _forceLookSpeed = 2f;
    [SerializeField] private float _forceMoveSpeed = 5f;
    
    [Header("Debug Settings")] 
    [SerializeField] private bool _showGizmos = false;
    
    [Header("References Settings")] 
    private PlayerInput _playerInput;
    [SerializeField] private Camera _camera;
    
    private float _pitch;
    private float _yaw;
    private float _currentPitch;
    private float _currentYaw;

    private Coroutine _forceLookCoroutine;
    private bool _isForcedLooking;
    
    private Coroutine _forceMoveCoroutine;
    private bool _isForcedMoving;

    private Vector3 startingPosition;
    private Vector2 lookDir;

    private void Awake()
    {
        startingPosition = transform.localPosition;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        lookDir = _playerInput.actions["Look"].ReadValue<Vector2>();
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
        _yaw += lookDir.x * _lookSensitivity * Time.deltaTime;
        _pitch -= lookDir.y * _lookSensitivity * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -_upDownLookLimit, _upDownLookLimit);

        float t = 1f - Mathf.Exp(-_lookLerpSpeed * Time.deltaTime);
        _currentYaw = Mathf.Lerp(_currentYaw, _yaw, t);
        _currentPitch = Mathf.Lerp(_currentPitch, _pitch, t);

        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
    }

    private void ForceLookInDirection(Vector3 targetPosition)
    {
        if (_forceLookCoroutine != null)
            StopCoroutine(_forceLookCoroutine);
        
        _forceLookCoroutine = StartCoroutine(RotateCameraInDirectionRoutine(targetPosition));
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
    
    private IEnumerator RotateCameraInDirectionRoutine(Vector3 targetPosition)
    {
        _isForcedLooking = true;
 
        if (_showGizmos)
            Debug.DrawRay(_camera.transform.position, targetPosition - _camera.transform.position, Color.aquamarine, 5f);
 
        float targetYaw = 0f;
        float targetPitch = 0f;
 
        while (true)
        {
            Vector3 direction = targetPosition - _camera.transform.position;
 
            targetYaw   = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            targetPitch = -Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;
 
            _currentYaw   = Mathf.LerpAngle(_currentYaw,   targetYaw,   Time.deltaTime * _forceLookSpeed);
            _currentPitch = Mathf.LerpAngle(_currentPitch, targetPitch,  Time.deltaTime * _forceLookSpeed);
 
            transform.rotation              = Quaternion.Euler(0f, _currentYaw, 0f);
            _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
 
            float angle = Quaternion.Angle(
                Quaternion.Euler(_currentPitch, _currentYaw, 0f),
                Quaternion.Euler(targetPitch, targetYaw, 0f));
 
            if (angle <= 0.1f)
                break;
 
            yield return null;
        }
        
        // Snapping to final rotation
        _currentYaw = targetYaw;
        _currentPitch = targetPitch;
        _yaw = targetYaw;
        _pitch = targetPitch;
        
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        
        StopForceLook();
    }
    
    private void ForceMoveCamera(Vector3 targetPosition)
    {
        if (_forceMoveCoroutine != null)
            StopCoroutine(_forceMoveCoroutine);

        _forceMoveCoroutine = StartCoroutine(MoveCameraToPositionRoutine(targetPosition));
    }

    private void OnStopForceMoveCamera() => StopForceMoveCamera(true);
    
    private void StopForceMoveCamera(bool returnToPosition)
    {
        if (_forceMoveCoroutine != null)
        {
            StopCoroutine(_forceMoveCoroutine);
            _forceMoveCoroutine = null;
        }

        if (returnToPosition)
            transform.localPosition = startingPosition;
        
        _isForcedMoving = false;

        Debug.Log("Stop Force Move Camera");
    }

    private IEnumerator MoveCameraToPositionRoutine(Vector3 targetPosition)
    {
        _isForcedMoving = true;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * _forceMoveSpeed
            );

            if (_showGizmos)
                Debug.DrawLine(transform.position, targetPosition, Color.cyan);

            yield return null;
        }
        
        transform.position = targetPosition;

        StopForceMoveCamera(false);
    }
}