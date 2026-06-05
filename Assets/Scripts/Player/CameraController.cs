using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour, ICameraRotation
{
    [Header("Vision Settings")]
    [SerializeField] private float lookSensitivity = 100f;
    [SerializeField] private float upDownPitchLimit = 70f;
    [SerializeField] private float lookLerpSpeed = 10f;
    
    [Header("Debug Settings")] 
    [SerializeField] private bool showGizmos = true;
    
    [Header("References Settings")] 
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private Camera playerCamera;
    
    private PlayerInput _playerInput;
    private CameraContext _cameraContext;
    private ICameraStrategy _cameraStrategy;
    
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float CurrentYaw { get; set; }
    public float CurrentPitch { get; set; }
    
    public Camera MainCamera => playerCamera;

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        
        _cameraContext = new CameraContext
        {
            CameraTransform = playerCamera.transform,
            PlayerTransform = transform,
            LookAction = _playerInput.actions.FindAction(lookActionName),
            LookLerpSpeed = lookLerpSpeed,
            LookSensitivity = lookSensitivity,
            UpDownPitchLimit = upDownPitchLimit
        };
    }

    private void LateUpdate()
    {
        _cameraStrategy?.Look(_cameraContext);
    }

    public void SetCameraStrategy(ICameraStrategy newStrategy)
    {
        Log.Info($"Camera Strategy -> {newStrategy.GetType().Name}");
        _cameraStrategy?.Exit(_cameraContext);
        _cameraStrategy = newStrategy;
        _cameraStrategy?.Enter(_cameraContext);
    }
}