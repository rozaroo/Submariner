using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour, ICameraRotation
{
    [Header("Vision Settings")]
    [SerializeField] private float lookSensitivity = 100f;
    [SerializeField] private float upDownLookLimit = 70f;
    [SerializeField] private float lookLerpSpeed = 10f;
    
    [Header("Debug Settings")] 
    [SerializeField] private bool showGizmos = true;
    
    [Header("References Settings")] 
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private Camera playerCamera;
    
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float CurrentYaw { get; set; }
    public float CurrentPitch { get; set; }

    private StateMachine _stateMachine;
    private PlayerInput _playerInput;
    private Coroutine _forceLookCoroutine;
    
    private Coroutine _forceMoveCoroutine;
    
    private Vector3 _startingPosition;
    private InputAction _lookAction;
    
    public Camera MainCamera { get => playerCamera; private set => playerCamera = value; }

    private void Awake()
    {
        _startingPosition = playerCamera.transform.localPosition;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        InputAction lookAction = _playerInput.actions.FindAction(lookActionName);
        
        CameraContext context = new CameraContext(this,
            playerCamera.transform,
            transform,
            lookSensitivity, 
            upDownLookLimit, 
            lookLerpSpeed, 
            lookAction);
        
        _stateMachine = new StateMachine();
        CameraLookState lookState = new CameraLookState(context);
        
        _stateMachine.ChangeState(lookState);
    }
    
    private void LateUpdate()
    {
        _stateMachine.LateUpdate();
    }

    #region ForceMoveCamera

    public void ForceMoveCamera(Vector3 targetPosition, float duration = 1.0f)
    {
        if (_forceMoveCoroutine != null) StopCoroutine(_forceMoveCoroutine);
        _forceMoveCoroutine = StartCoroutine(MoveCameraToPositionRoutine(targetPosition, duration));
    }
    
    private IEnumerator MoveCameraToPositionRoutine(Vector3 targetPosition, float duration)
    {
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
    }
    
    public void ReturnToStartingPosition(float duration = 1.0f)
    {
        StopForceLook();
        if (_forceMoveCoroutine != null) StopCoroutine(_forceMoveCoroutine);
        _forceMoveCoroutine = StartCoroutine(ReturnToLocalPositionRoutine(duration));
    }
    
    
    private IEnumerator ReturnToLocalPositionRoutine(float duration)
    {
        Vector3 startLocalPos = playerCamera.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            playerCamera.transform.localPosition = Vector3.Lerp(startLocalPos, _startingPosition, t);
            yield return null;
        }

        playerCamera.transform.localPosition = _startingPosition;
        StopForceMoveCamera(false);
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
    }
    
    private void ForceRotationInstant(float newYaw, float newPitch)
    {
        Yaw = newYaw;
        Pitch = newPitch;
        CurrentYaw = newYaw;
        CurrentPitch = newPitch;
    }
    
    private IEnumerator RotateCameraInDirectionRoutine(Vector3 targetPosition, float duration)
    {
 
        if (showGizmos)
            Debug.DrawRay(playerCamera.transform.position, targetPosition - playerCamera.transform.position, Color.aquamarine, 5f);
 
        float startYaw = CurrentYaw;
        float startPitch = CurrentPitch;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            
            Vector3 direction = targetPosition - playerCamera.transform.position;
            float targetYaw   = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;

            CurrentYaw   = Mathf.LerpAngle(startYaw, targetYaw, t);
            CurrentPitch = Mathf.LerpAngle(startPitch, targetPitch, t);
 
            transform.rotation              = Quaternion.Euler(0f, CurrentYaw, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
 
            yield return null;
        }
        
        Vector3 finalDirection = targetPosition - playerCamera.transform.position;
        float finalYaw   = Mathf.Atan2(finalDirection.x, finalDirection.z) * Mathf.Rad2Deg;
        float finalPitch = -Mathf.Asin(finalDirection.normalized.y) * Mathf.Rad2Deg;
        
        // Final Snap (Security)
        ForceRotationInstant(finalYaw, finalPitch);
        
        transform.rotation = Quaternion.Euler(0f, CurrentYaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
        
        StopForceLook();
    }

    #endregion
}