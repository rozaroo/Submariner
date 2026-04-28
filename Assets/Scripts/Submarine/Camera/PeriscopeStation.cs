using System;
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
    
    private PlayerCharacter _currentPlayer;

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
            if (cancelAction != null) 
            {
                cancelAction.started += OnCancelStarted;
            }
        }
        if (onPeriscopePossess != null)
        {
            onPeriscopePossess.RaiseEvent(); 
        }
    }

    public void UnPossess()
    {
        if (_currentPlayer != null)
        {
            if (_currentPlayer.Input != null)
            {
                var clickAction = _currentPlayer.Input.actions[takePhotoActionName];
                clickAction.started -= OnPhotoClickStarted;

                var cancelAction = _currentPlayer.Input.actions[exitActionName];
                if (cancelAction != null) 
                {
                    cancelAction.started -= OnCancelStarted;
                }
                
                _currentPlayer.Input.SwitchCurrentActionMap(playerMapName);
            }
            _currentPlayer = null;
        }
    }

    #endregion

    #region PhotoActions

    private void OnPhotoClickStarted(InputAction.CallbackContext context)
    {
        if (activeCamera != null)
        {
            activeCamera.TryTakePhoto(); 
        }
    }

    private void OnCancelStarted(InputAction.CallbackContext context)
    {
        UnPossess();
    }

    #endregion
}