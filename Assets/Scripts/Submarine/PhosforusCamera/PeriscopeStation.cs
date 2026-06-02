using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PeriscopeStation : MonoBehaviour, IInteractable, IPossessable 
{
    [Header("Camera Connection")]
    [SerializeField] private PhosphorusCamera activeCamera; 
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;
    
    [Header("Input Settings")]
    [SerializeField] private string takePhotoActionName; 
    [SerializeField] private string exitActionName; 

    [Header("Event Channels")]
    [SerializeField] private BaseEventChannelSO onPeriscopePossess;
    [SerializeField] private BaseEventChannelSO onPeriscopeUnpossess;

    [SerializeField] private PeriscopeFlash3D flashEffect;

    private PlayerCharacter _currentPlayer;

    [SerializeField] private string lookActionName = "Look";
    private Coroutine _exitRoutine;

    private void Awake()
    {
        enabled = false;
    }
    
    public void Interact(PlayerCharacter player)
    {
        _currentPlayer = player;
        Possess();
    }

    #region PosessionLogic

    public void Possess()
    {
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
        activeCamera.BeginPeriscopeControl();
        activeCamera.EnableCamera();
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
        if (activeCamera != null)
        {
            activeCamera.EndPeriscopeControl();
            activeCamera.ForceDisable();
        }
        if (onPeriscopeUnpossess != null) onPeriscopeUnpossess.RaiseEvent();
        _currentPlayer = null;
    }

    #endregion

    #region PhotoActions

    private void OnPhotoClickStarted(InputAction.CallbackContext context)
    {
        if (activeCamera == null) return;
        if (!activeCamera.CanTakePhoto())
        {
            Log.Info("Photo Input Blocked - No Energy");
            return;
        }
        activeCamera.TryTakePhoto();
        if (flashEffect != null) flashEffect.PlayFlash();
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(ExitAfterPhoto());
    }

    private void OnCancelStarted(InputAction.CallbackContext context)
    {
        UnPossess();
    }
    private IEnumerator ExitAfterPhoto()
    {
        yield return new WaitForSeconds(activeCamera.GetVisibleDuration());
        _exitRoutine = null;
        UnPossess();
    }
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        activeCamera.Rotate(delta);
    }
    #endregion
}