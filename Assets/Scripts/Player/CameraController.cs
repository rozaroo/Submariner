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
    
    private StateMachine _cameraStateMachine;
    private CameraLookState _cameraLookState;
    private CameraForceLookState _cameraForceLookState;
    private CameraLockedState _cameraLockedState;
    
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
        
        _cameraStateMachine = new StateMachine();
        _cameraLookState = new CameraLookState(context);
        _cameraLockedState = new CameraLockedState();
        _cameraForceLookState = new CameraForceLookState(context, false, null);
        
        _cameraStateMachine.ChangeState(_cameraLookState);
    }

    private void Update()
    {
        _cameraStateMachine.Update(); //TODO: Always force to put _S
    }
    
    private void LateUpdate()
    {
        _cameraStateMachine.LateUpdate();
    }

    #region State Machine

    public void ForceMoveLookCamera(Vector3 targetMovePosition, Vector3 targetLookPosition, float duration)
    {
        _cameraForceLookState.SetupState(targetMovePosition, targetLookPosition, 
            duration, _cameraLockedState, isReturning: false);
        _cameraStateMachine.ChangeState(_cameraForceLookState);
    }

    public void ReturnToStartingPosition(float duration)
    {
        _cameraForceLookState.SetupState(Vector3.zero, Vector3.zero, 
            duration, _cameraLookState, isReturning: true);
        _cameraStateMachine.ChangeState(_cameraForceLookState);
    }

    #endregion
}