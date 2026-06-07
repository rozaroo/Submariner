using UnityEngine;
using UnityEngine.InputSystem;

public class NavigationStation : MonoBehaviour, IInteractable, IPossessable
{
    [Header("Visual Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private CursorLockMode cursorLockMode;
    [SerializeField] private bool showMouseCursor;
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;

    [Header("Property Settings")] 
    [SerializeField] private MapUIManager mapUI;
    
    [Header("Input Settings")]
    [SerializeField] private string exitActionName;
    
    private PlayerCharacter _currentPlayer;
    
    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    public void Interact(PlayerCharacter player)
    {
        player.OnPossessionState(this);
    }

    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        mapUI.MapCanvas.worldCamera = _currentPlayer.CamController.MainCamera;
        var exitAction = _currentPlayer.Input.actions[exitActionName];
        exitAction.started += OnExitPerformed;
        
        enabled = true;
    }

    public void UnPossess()
    {
        var exitAction = _currentPlayer.Input.actions[exitActionName];
        exitAction.started -= OnExitPerformed;
        
        mapUI.MapCanvas.worldCamera = null;
        _currentPlayer = null;
        enabled = false;
    }
    
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        _currentPlayer.OnUnPossessionState(this);
    }
}
