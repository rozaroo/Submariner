using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

public class DrainageStation : MonoBehaviour, IPossessable, IInteractable
{
    [Header("Visual Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;
    
    [Header("Input Settings")]
    [SerializeField] private string clickActionName;
    [SerializeField] private string pointerDeltaActionName;
    [SerializeField] private string exitActionName;
    
    [Header("Minigame References")]
    [SerializeField] private List<ButtonStation> buttons;
    [SerializeField] private LeverStation mainLever;
    
    [Header("Minigame Settings")]
    [SerializeField] private int buttonsToUnlock = 3;

    [Header("Drainage Settings")] 
    [SerializeField] private float drainagePercentage = 1f;
    [SerializeField] private EnergyStatus energyStatus = EnergyStatus.Full;

    [Header("Event Channels")]
    [SerializeField] private EnergyStatusEventSO onEnergyStatusChange;
    [SerializeField] private DrainagePropertyEventChannelSO onDrainageStatusChanged;
    
    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private IStationControl _currentDraggedControl;
    private int _playableButtonsCount;
    private int _buttonsPressedCount = 0;
    private Vector2 _mouseDelta;

    private void Awake()
    {
        enabled = false;
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.OnActivation += OnAnyButtonHit;
            }
        }
        if (mainLever != null)
        {
            mainLever.OnActivation += StartDrainageSequence;
        }
    }

    private void OnEnable()
    {
        onEnergyStatusChange.OnEventRaised += OnEnergyStatusChanged;
    }

    private void OnDisable()
    {
        onEnergyStatusChange.OnEventRaised -= OnEnergyStatusChanged;
    }

    private void Update()
    {
        if (_currentDraggedControl != null && Mouse.current != null)
        {
            _mouseDelta = _currentPlayer.Input.actions[pointerDeltaActionName].ReadValue<Vector2>();
            float mouseDeltaY = _mouseDelta.y;
            _currentDraggedControl.OnActionDrag(mouseDeltaY);
        }
    }   

    public void Interact(PlayerCharacter player)
    {
        _currentPlayer = player;
        _playerCamera = player.CamController.MainCamera;
        Possess();
    }

    #region PosessionLogic

    public void Possess()
    {
        if (_currentPlayer.Input != null)
        {
            _currentPlayer.Input.SwitchCurrentActionMap(stationMapName);
            var clickAction = _currentPlayer.Input.actions[clickActionName];
            clickAction.started += OnClickStarted;
            clickAction.canceled += OnClickCanceled;
            var exitAction = _currentPlayer.Input.actions[exitActionName];
            exitAction.started += OnExitPerformed;
        }
        
        if (_currentPlayer.CamController != null)
        {
            _currentPlayer.CamController.ForceMoveCamera(cameraAnchor.position, transitionDuration);
            _currentPlayer.CamController.ForceLookInDirection(directionAnchor.position, transitionDuration);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        enabled = true;
        SetupMiniGame();
    }
    
    public void UnPossess()
    {
        if (_currentPlayer != null)
        {
            if (_currentPlayer.Input != null)
            {
                var clickAction = _currentPlayer.Input.actions[clickActionName];
                clickAction.started -= OnClickStarted;
                clickAction.canceled -= OnClickCanceled;
                
                var exitAction = _currentPlayer.Input.actions[exitActionName];
                exitAction.started -= OnExitPerformed;
                
                _currentPlayer.Input.SwitchCurrentActionMap(playerMapName);
            }
            if (_currentPlayer.CamController != null) 
            {
                _currentPlayer.CamController.ReturnToStartingPosition(transitionDuration);
                _currentPlayer.CamController.enabled = true;
            }
            _currentPlayer = null;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _currentDraggedControl = null;
        _currentPlayer = null;
        _playerCamera = null;
        RestartStation();
        enabled = false;
    }

    #endregion

    #region InputActions

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _playerCamera.ScreenPointToRay(mousePos);
                
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            if (hit.collider.TryGetComponent(out IStationControl control))
            {
                _currentDraggedControl = control;
                _currentDraggedControl.OnActionDown();
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (_currentDraggedControl != null)
        {
            _currentDraggedControl.OnActionUp();
            _currentDraggedControl = null;
        }
    }
    
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        UnPossess();
    }
    #endregion

    #region MinigameLogic

    private void SetupMiniGame()
    {
        RestartStation();
        SelectRandomButtons();
        CheckButtonAvailability();
    }

    private void SelectRandomButtons()
    {
        List<ButtonStation> availableButtons = new List<ButtonStation>(buttons);
        _playableButtonsCount = 0;
        for (int i = 0; i < buttonsToUnlock && availableButtons.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableButtons.Count);
            availableButtons[randomIndex].Unlock();
            availableButtons.RemoveAt(randomIndex);
            _playableButtonsCount++;
        }
    }
    
    private void CheckButtonAvailability()
    {
        if (_playableButtonsCount == 0)
        {
            Debug.LogError("NOT ENOUGH BUTTONS IN THE SCENE FOR THE MINIGAME!");
            StartDrainageSequence();
        }
    }

    private void OnAnyButtonHit()
    {
        _buttonsPressedCount++;
        if (_buttonsPressedCount >= _playableButtonsCount)
        {
            mainLever.Unlock();
        }
    }

    #endregion
    
    #region DrainageLogic
    
    private void StartDrainageSequence()
    {
        onDrainageStatusChanged.RaiseEvent(CreateDrainageProperty());
        Debug.Log("MINIGAME FINISHED!");
        UnPossess();
    }
    
    private DrainagePropertyData CreateDrainageProperty()
    {
        return new DrainagePropertyData
        {
            drainagePercentage = drainagePercentage
        };
    }
    
    private void SetDrainageStatus()
    {
        switch (energyStatus)
        {
            case EnergyStatus.Full:
                drainagePercentage = 1f;
                break;
            case EnergyStatus.Low:
                drainagePercentage = 0.5f;
                break;
            case EnergyStatus.Empty:
                drainagePercentage = 0f;
                break; 
        }
    }
    
    private void OnEnergyStatusChanged(EnergyStatus status)
    {
        energyStatus = status;
        SetDrainageStatus();
    }

    private void RestartStation()
    {
        _buttonsPressedCount = 0;
        foreach (var button in buttons)
        {
            button.RestartButton();
        }
        mainLever.RestartButton();
    }
    #endregion
    
    private void OnDestroy()
    {
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.OnActivation -= OnAnyButtonHit;
            }
        }

        if (mainLever != null)
        {
            mainLever.OnActivation -= StartDrainageSequence;
        }
    }
}