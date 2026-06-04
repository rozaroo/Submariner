using UnityEngine;
using UnityEngine.InputSystem;

public class PeriscopeStation : MonoBehaviour, IInteractable, IPossessable 
{
    [Header("Camera Connection")]
    [SerializeField] private PeriscopeCameraAnchorSO _periscopeCameraAnchorSo;
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;
    
    [Header("Input Settings")]
    [SerializeField] private string takePhotoActionName; 
    [SerializeField] private string exitActionName; 

    [Header("Event Channels")]
    [SerializeField] private BaseEventChannelSO onPeriscopePossess;
    [SerializeField] private BaseEventChannelSO onPeriscopeUnpossess;
    
    [Header("Inputs")]
    [SerializeField] private string lookActionName = "Look";

    private PhosphorusCamera _componentCamera;
    private PlayerCharacter _currentPlayer;
    private Coroutine _exitRoutine;
    
    public string MapName => stationMapName;
    public Transform CameraAnchor { get; }
    public Transform DirectionAnchor { get; }
    public float TransitionDuration { get; }

    private void Awake()
    {
        enabled = false;
    }
    
    public void Interact(PlayerCharacter player)
    {
        if (_periscopeCameraAnchorSo.phosphorusCameraComponent == null)
        {
            Log.Warning("[Periscope Station]: No PhosphorusCamera]");
            return;
        }
        player.OnPossessionState(this, false);
    }
    
    #region PosessionLogic
    
    
    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _periscopeCameraAnchorSo.playerCamera = player.camController.MainCamera;
        
        InputAction clickAction = _currentPlayer.input.actions[takePhotoActionName];
        clickAction.started += OnPhotoClickStarted;
            
        InputAction cancelAction = _currentPlayer.input.actions[exitActionName];
        cancelAction.started += OnExitStarted;
            
        InputAction lookAction = _currentPlayer.input.actions[lookActionName];
        lookAction.performed += OnLookPerformed;
        
        enabled = true;
        if (onPeriscopePossess != null) onPeriscopePossess.RaiseEvent();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.BeginPeriscopeControl();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.EnableCamera();
    }
    
    public void UnPossess()
    {
        InputAction clickAction = _currentPlayer.input.actions[takePhotoActionName];
        clickAction.started -= OnPhotoClickStarted;
        
        InputAction cancelAction = _currentPlayer.input.actions[exitActionName];
        cancelAction.started -= OnExitStarted;
        
        InputAction lookAction = _currentPlayer.input.actions[lookActionName];
        lookAction.performed -= OnLookPerformed;
        
        _periscopeCameraAnchorSo.phosphorusCameraComponent.EndPeriscopeControl();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.ForceDisable();
        
        if (onPeriscopeUnpossess != null) onPeriscopeUnpossess.RaiseEvent();
        _currentPlayer = null;
        enabled = false;
    }

    #endregion

    #region PhotoActions

    private void OnPhotoClickStarted(InputAction.CallbackContext context)
    {
        if (_periscopeCameraAnchorSo.phosphorusCameraComponent  == null) return;
        
        if (!_periscopeCameraAnchorSo.phosphorusCameraComponent.CanTakePhoto())
        {
            return;
        }

        Log.Info("[PeriscopeStation] Taking Photo");
        
        _periscopeCameraAnchorSo.phosphorusCameraComponent.TryTakePhoto();
    }

    private void OnExitStarted(InputAction.CallbackContext context)
    {
        _currentPlayer.OnUnPossessionState();
    }
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        _periscopeCameraAnchorSo.phosphorusCameraComponent.Rotate(delta);
    }
    
    #endregion
}