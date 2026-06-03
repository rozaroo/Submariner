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
    
    private StateMachine _stateMachine;
    private CameraLookState _cameraLookState;
    private CameraForceLookState _cameraForceLookState;
    
    private PlayerInput _playerInput;
    private InputAction _lookAction;
    
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float CurrentYaw { get; set; }
    public float CurrentPitch { get; set; }
    public Camera MainCamera => playerCamera;

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
        _cameraLookState = new CameraLookState(context);
        _cameraForceLookState = new CameraForceLookState(context, _stateMachine);
        
        _stateMachine.ChangeState(_cameraLookState);
    }
    
    private void LateUpdate()
    {
        _stateMachine.LateUpdate();
    }

    public void ForceMoveLookCamera(Vector3 targetMovePosition, Vector3 targetLookPosition, float duration)
    {
        _cameraForceLookState.SetupTransition(targetMovePosition, targetLookPosition, duration, isReturning: false);
        _stateMachine.ChangeState(_cameraForceLookState);
    }

    public void ReturnToStartingPosition(float duration)
    {
        _cameraForceLookState.SetupTransition(Vector3.zero, Vector3.zero, duration, isReturning: true);
        _stateMachine.ChangeState(_cameraForceLookState);
    }
}