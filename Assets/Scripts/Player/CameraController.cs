using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float lookSensitivity = 100f;
    [SerializeField] private float upDownLookLimit = 70f;
    [SerializeField] private float lookLerpSpeed = 10f;
    
    [Header("Debug Settings")] 
    [SerializeField] private bool showGizmos = true;
    
    [Header("References Settings")] 
    private PlayerInput _playerInput;
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private Camera playerCamera;
    
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
    private InputAction _lookAction;
    
    public Camera MainCamera { get => playerCamera; private set => playerCamera = value; }

    private void Awake()
    {
        _startingPosition = playerCamera.transform.localPosition;
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
        float deltaYaw = _lookDir.x * lookSensitivity * Time.deltaTime;
        float deltaPitch = -_lookDir.y * lookSensitivity * Time.deltaTime;
        _yaw += deltaYaw;
        _pitch += deltaPitch;
        _pitch = Mathf.Clamp(_pitch, -upDownLookLimit, upDownLookLimit);

        if (lookLerpSpeed >= 50)
        {
            _currentYaw = _yaw;
            _currentPitch = _pitch;
        }
        else
        {
            float t = 1f - Mathf.Exp(-lookLerpSpeed * Time.deltaTime);
            _currentYaw = Mathf.LerpAngle(_currentYaw, _yaw, t);
            _currentPitch = Mathf.LerpAngle(_currentPitch, _pitch, t);
        }

        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
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

        Vector3 startPos = playerCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            playerCamera.transform.position = Vector3.Lerp(startPos, targetPosition, t);

            if (showGizmos)
                Debug.DrawLine(playerCamera.transform.position, targetPosition, Color.cyan);

            yield return null;
        }
        
        playerCamera.transform.position = targetPosition;
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
            playerCamera.transform.localPosition = _startingPosition;
        
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
    
    private void ForceRotationInstant(float newYaw, float newPitch)
    {
        _yaw = newYaw;
        _currentYaw = newYaw;
        _pitch = newPitch;
        _currentPitch = newPitch;
    }
    
    private IEnumerator RotateCameraInDirectionRoutine(Vector3 targetPosition, float duration)
    {
        _isForcedLooking = true;
 
        if (showGizmos)
            Debug.DrawRay(playerCamera.transform.position, targetPosition - playerCamera.transform.position, Color.aquamarine, 5f);
 
        float startYaw = _currentYaw;
        float startPitch = _currentPitch;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            
            Vector3 direction = targetPosition - playerCamera.transform.position;
            float targetYaw   = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;

            _currentYaw   = Mathf.LerpAngle(startYaw, targetYaw, t);
            _currentPitch = Mathf.LerpAngle(startPitch, targetPitch, t);
 
            transform.rotation              = Quaternion.Euler(0f, _currentYaw, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
 
            yield return null;
        }
        
        Vector3 finalDirection = targetPosition - playerCamera.transform.position;
        float finalYaw   = Mathf.Atan2(finalDirection.x, finalDirection.z) * Mathf.Rad2Deg;
        float finalPitch = -Mathf.Asin(finalDirection.normalized.y) * Mathf.Rad2Deg;
        
        // Final Snap (Security)
        ForceRotationInstant(finalYaw, finalPitch);
        
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        
        StopForceLook();
    }

    #endregion
}