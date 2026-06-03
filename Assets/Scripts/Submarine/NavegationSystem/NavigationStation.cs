using UnityEngine;
using UnityEngine.InputSystem;

public class NavigationStation : MonoBehaviour, IInteractable, IPossessable
{
    [Header("Visual Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;

    [Header("Property Settings")] 
    [SerializeField] private MapUIManager mapUI;
    
    [Header("Input Settings")]
    [SerializeField] private string exitActionName;

    [Header("Event Channels")] 
    [SerializeField] private BaseEventChannelSO onPossessNavigationStation;
    [SerializeField] private BaseEventChannelSO onUnPossessNavigationStation;
    
    private PlayerCharacter _currentPlayer;
    public void Interact(PlayerCharacter player)
    {
        _currentPlayer = player;
        Possess();
    }

    public void Possess()
    {
        if (_currentPlayer.input != null)
        {
            _currentPlayer.input.SwitchCurrentActionMap(stationMapName);
            var exitAction = _currentPlayer.input.actions[exitActionName];
            exitAction.started += OnExitPerformed;
        }
        
        if (_currentPlayer.camController != null)
        {
            _currentPlayer.camController.ForceMoveCamera(cameraAnchor.position, transitionDuration);
            _currentPlayer.camController.ForceLookInDirection(directionAnchor.position, transitionDuration);
            mapUI.MapCanvas.worldCamera = _currentPlayer.camController.MainCamera;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        enabled = true;
    }

    public void UnPossess()
    {
        if (_currentPlayer != null)
        {
            if (_currentPlayer.input != null)
            {
                var exitAction = _currentPlayer.input.actions[exitActionName];
                exitAction.started -= OnExitPerformed;
                _currentPlayer.input.SwitchCurrentActionMap(playerMapName);
            }
            if (_currentPlayer.camController != null) 
            {
                _currentPlayer.camController.ReturnToStartingPosition(transitionDuration);
                _currentPlayer.camController.enabled = true;
            }
            mapUI.MapCanvas.worldCamera = null;
            _currentPlayer = null;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _currentPlayer = null;
    }
    
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        UnPossess();
    }
}
