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

    private void Awake()
    {
        enabled = false;
    }
    
    public void Interact(PlayerCharacter player)
    {
        _currentPlayer = player;
        if (_periscopeCameraAnchorSo != null && player != null)
        {
            Camera playerCam = player.CamController.MainCamera;
            if (playerCam != null)
            {
                _periscopeCameraAnchorSo.playerCamera = playerCam;
            }
        }
        Possess();
    }

    #region PosessionLogic

    public void Possess()
    {
        if (_periscopeCameraAnchorSo.phosphorusCameraComponent == null)
        {
            Log.Warning("[Periscope Station]: No PhosphorusCamera]");
            return;
        }
        enabled = true;
        if (_currentPlayer.Input != null)
        {
            _currentPlayer.Input.SwitchCurrentActionMap(stationMapName);
            var clickAction = _currentPlayer.Input.actions[takePhotoActionName];
            clickAction.started += OnPhotoClickStarted;
            
            var cancelAction = _currentPlayer.Input.actions[exitActionName];
            if (cancelAction != null) cancelAction.started += OnCancelStarted;
            
            var lookAction = _currentPlayer.Input.actions[lookActionName];
            lookAction.performed += OnLookPerformed;
        }
        if (onPeriscopePossess != null) onPeriscopePossess.RaiseEvent();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.BeginPeriscopeControl();
        _periscopeCameraAnchorSo.phosphorusCameraComponent.EnableCamera();
    }

    public void UnPossess()
    {
        enabled = false;
        if (_currentPlayer != null)
        {
            if (_currentPlayer.Input != null)
            {
                var clickAction = _currentPlayer.Input.actions[takePhotoActionName];
                clickAction.started -= OnPhotoClickStarted;
                var cancelAction = _currentPlayer.Input.actions[exitActionName];
                if (cancelAction != null) cancelAction.started -= OnCancelStarted;
                var lookAction = _currentPlayer.Input.actions[lookActionName];
                lookAction.performed -= OnLookPerformed;
                _currentPlayer.Input.SwitchCurrentActionMap(playerMapName);
            }
        }
        if (_periscopeCameraAnchorSo.phosphorusCameraComponent  != null)
        {
            _periscopeCameraAnchorSo.phosphorusCameraComponent.EndPeriscopeControl();
            _periscopeCameraAnchorSo.phosphorusCameraComponent.ForceDisable();
        }
        if (onPeriscopeUnpossess != null) onPeriscopeUnpossess.RaiseEvent();
        _currentPlayer = null;
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

    private void OnCancelStarted(InputAction.CallbackContext context)
    {
        UnPossess();
    }
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        _periscopeCameraAnchorSo.phosphorusCameraComponent.Rotate(delta);
    }
    
    #endregion
}