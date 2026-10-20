using UnityEngine;
using UnityEngine.InputSystem;

public class PeriscopeStation : MonoBehaviour, IInteractable, IPossessable 
{
    [Header("Visual Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private CursorLockMode cursorLockMode;
    [SerializeField] private bool showMouseCursor;
    
    [Header("Camera Connection")]
    [SerializeField] private PeriscopeCameraAnchorSO _periscopeCameraAnchorSo;
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;
    
    [Header("Input Settings")]
    [SerializeField] private string takePhotoActionName; 
    [SerializeField] private string exitActionName;
    [SerializeField] private string resetStationName;
    
    [Header("Interaction")]
    [SerializeField] private InteractionUIData interactionUIData;

    private PhosphorusCamera _componentCamera;
    private PlayerCharacter _currentPlayer;
    private Coroutine _exitRoutine;
    
    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

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
        player.OnPossessionState(this);
    }
    
    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        return new InteractionFeedback("Enter Periscope",interactionUIData);
    }

    #region PosessionLogic
    
    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _periscopeCameraAnchorSo.playerCamera = player.CamController.MainCamera;
        
        InputAction clickAction = _currentPlayer.Input.actions[takePhotoActionName];
        clickAction.started += OnPhotoClickStarted;
        
        InputAction cancelAction = _currentPlayer.Input.actions[exitActionName];
        cancelAction.started += OnExitStarted;

        InputAction resetAction = _currentPlayer.Input.actions[resetStationName];
        resetAction.started += OnResetStarted;

        enabled = true;
        GameEventChannel<OnPeriscopePossess>.RaiseEvent(new OnPeriscopePossess());
        
        _periscopeCameraAnchorSo.phosphorusCameraComponent.BeginPeriscopeControl();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.EnableCamera();
    }
    
    public void UnPossess()
    {
        InputAction clickAction = _currentPlayer.Input.actions[takePhotoActionName];
        clickAction.started -= OnPhotoClickStarted;
        
        InputAction cancelAction = _currentPlayer.Input.actions[exitActionName];
        cancelAction.started -= OnExitStarted;

        InputAction resetAction = _currentPlayer.Input.actions[resetStationName];
        resetAction.started -= OnResetStarted;

        _periscopeCameraAnchorSo.phosphorusCameraComponent.EndPeriscopeControl();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.ForceDisable();
            
        GameEventChannel<OnPeriscopeUnPossess>.RaiseEvent(new OnPeriscopeUnPossess());
        _currentPlayer = null;
        enabled = false;
    }

    #endregion

    #region PhotoActions

    private void OnPhotoClickStarted(InputAction.CallbackContext context)
    {
        if (StationClickBlocker.TryConsumeClick(_currentPlayer?.CamController.MainCamera))
        {
            return;
        }

        if (_periscopeCameraAnchorSo.phosphorusCameraComponent == null) return;
        
        if (!_periscopeCameraAnchorSo.phosphorusCameraComponent.CanTakePhoto())
        {
            SFXManager.PostEvent("Start_PhosphorusCameraCooldown", gameObject);
            return;
        }
        
        Log.Info("[PeriscopeStation] Taking Photo");
        _periscopeCameraAnchorSo.phosphorusCameraComponent.TryTakePhoto();
    }

    private void OnExitStarted(InputAction.CallbackContext context)
    {
        _currentPlayer.OnUnPossessionState(this);
    }
    private void OnResetStarted(InputAction.CallbackContext context)
    {
        if (_periscopeCameraAnchorSo.phosphorusCameraComponent == null) return;
        _periscopeCameraAnchorSo.phosphorusCameraComponent.ResetCameraRotation();
    }

    #endregion
}
