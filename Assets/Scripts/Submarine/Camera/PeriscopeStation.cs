using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PeriscopeStation : MonoBehaviour, IInteractable, IPossessable 
{
    [Header("Camera Connection")]
    [SerializeField] private PhosphorusCamera _activeCamera; 

    [Header("Input Settings")]
    [SerializeField] private string _TakePhotoActionName = "TakePhoto"; 
    [SerializeField] private string _exitActionName = "ExitStation"; 
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string _playerMapName = "Player";
    [SerializeField] private string _periscopeMapName = "Periscope";

    [Header("Event Channels")]
    [SerializeField] private BaseEventChannelSO _onPeriscopePossess;
    [SerializeField] private BaseEventChannelSO _onPeriscopeUnpossess;

    private PlayerInput _playerInput; 
    private PlayerCharacter _currentPlayer; 
    private bool _isPossessed = false;

    private void Update()
    {
        if (!_isPossessed || _playerInput == null) return;
        
        bool takePhoto = _playerInput.actions[_TakePhotoActionName].WasPressedThisFrame();

        if (takePhoto && _activeCamera != null)
        {
            _activeCamera.TryTakePhoto();
        }
        
        bool exitStation = _playerInput.actions[_exitActionName].WasPressedThisFrame();

        if (exitStation)
        {
            UnPossess();
        }
    }
    
    public void Interact(PlayerCharacter player)
    {
        _currentPlayer = player;
        _playerInput = _currentPlayer.GetComponent<PlayerInput>();
        
        Possess();
    }
    
    public void Possess()
    {
        _isPossessed = true;
        
        if (_playerInput != null)
        {
            _playerInput.SwitchCurrentActionMap(_periscopeMapName);
            if (_onPeriscopePossess != null)
            {
                _onPeriscopePossess.RaiseEvent();
            }
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found on the current player.");
        }
    }

    public void UnPossess()
    {
        _isPossessed = false;
        
        if (_onPeriscopeUnpossess != null)
        {
            _onPeriscopeUnpossess.RaiseEvent();
        }
        
        if (_currentPlayer != null)
        {
            _playerInput.SwitchCurrentActionMap(_playerMapName);
            _currentPlayer = null;
        }
    }
}