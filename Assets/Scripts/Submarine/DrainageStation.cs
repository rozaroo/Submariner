using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private List<ButtonStation> requiredButtons;
    [SerializeField] private LeverStation mainLever;
    
    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private IStationControl _currentDraggedControl;
    private int _buttonsPressedCount = 0;

    private void Awake()
    {
        enabled = false;
        
        foreach (var button in requiredButtons)
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
    
    private void Update()
    {
        if (_currentDraggedControl != null && Mouse.current != null)
        {
            float mouseDeltaY = _currentPlayer.Input.actions[pointerDeltaActionName].ReadValue<Vector2>().y;
            _currentDraggedControl.OnPointerDrag(mouseDeltaY);
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
                _currentDraggedControl.OnPointerDown();
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (_currentDraggedControl != null)
        {
            _currentDraggedControl.OnPointerUp();
            _currentDraggedControl = null;
        }
    }
    
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        UnPossess();
    }
    #endregion

    #region DrainageLogic

    private void OnAnyButtonHit()
    {
        _buttonsPressedCount++;
        
        if (_buttonsPressedCount >= requiredButtons.Count)
        {
            mainLever.UnlockLever();
        }
    }

    private void StartDrainageSequence()
    {
        //TODO: Drainage Sequence Logic
    }

    private void RestartStation()
    {
        _buttonsPressedCount = 0;
        foreach (var button in requiredButtons)
        {
            button.RestartButton();
        }
        mainLever.RestartButton();
    }
    #endregion
    
    private void OnDestroy()
    {
        foreach (var button in requiredButtons)
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