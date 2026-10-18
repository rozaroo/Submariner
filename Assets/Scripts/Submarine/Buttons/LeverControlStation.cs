using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gives a group of levers the same station flow as the other submarine
/// machinery: interact to move to the panel, then hold and drag a lever.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LeverControlStation : MonoBehaviour, IInteractable, IPossessable
{
    [Header("Station View")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.4f;
    [SerializeField] private CursorLockMode cursorLockMode = CursorLockMode.None;
    [SerializeField] private bool showMouseCursor = true;

    [Header("Station Input")]
    [SerializeField] private string stationMapName = "Station";
    [SerializeField] private string clickActionName = "ClickInteraction";
    [SerializeField] private string pointerDeltaActionName = "Pointer";
    [SerializeField] private string exitActionName = "ExitStation";
    [SerializeField, Min(0.1f)] private float leverRaycastDistance = 5f;

    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private InputAction _clickAction;
    private InputAction _pointerDeltaAction;
    private InputAction _exitAction;
    private ILeverControls _draggedLever;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    public static event Action<bool> OnLeverStationPossessionChanged;

    private void Awake()
    {
        // The component only needs to update while the player owns the station.
        enabled = false;
    }

    private void OnValidate()
    {
        if (cameraAnchor == null || directionAnchor == null)
        {
            Debug.LogWarning(
                $"[{nameof(LeverControlStation)}] Assign camera and direction anchors on {gameObject.name}.",
                this);
        }
    }

    public void Interact(PlayerCharacter player)
    {
        if (player == null || cameraAnchor == null || directionAnchor == null)
        {
            Log.Warning($"[{nameof(LeverControlStation)}] Cannot enter {gameObject.name}: station anchors are missing.");
            return;
        }

        player.OnPossessionState(this);
    }

    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _playerCamera = player.CamController.MainCamera;
        _clickAction = player.Input.actions.FindAction(clickActionName);
        _pointerDeltaAction = player.Input.actions.FindAction(pointerDeltaActionName);
        _exitAction = player.Input.actions.FindAction(exitActionName);

        if (_clickAction != null)
        {
            _clickAction.started += OnClickStarted;
            _clickAction.canceled += OnClickCanceled;
        }
        else Log.Error($"[{nameof(LeverControlStation)}] Action '{clickActionName}' was not found.");
        
        if (_exitAction != null) _exitAction.started += OnExitStarted;
        else Log.Error($"[{nameof(LeverControlStation)}] Action '{exitActionName}' was not found.");
        enabled = true;
        OnLeverStationPossessionChanged?.Invoke(true);
    }

    public void UnPossess()
    {
        if (_clickAction != null)
        {
            _clickAction.started -= OnClickStarted;
            _clickAction.canceled -= OnClickCanceled;
        }
        if (_exitAction != null) _exitAction.started -= OnExitStarted;
        OnLeverStationPossessionChanged?.Invoke(false);
        _draggedLever = null;
        _clickAction = null;
        _pointerDeltaAction = null;
        _exitAction = null;
        _playerCamera = null;
        _currentPlayer = null;
        enabled = false;
    }

    private void Update()
    {
        if (_draggedLever == null || _currentPlayer == null) return;

        if (_pointerDeltaAction == null) return;

        _draggedLever.OnActionDrag(_pointerDeltaAction.ReadValue<Vector2>().y);
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        _draggedLever = GetLeverUnderCursor();
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        _draggedLever = null;
    }

    private void OnExitStarted(InputAction.CallbackContext context)
    {
        _currentPlayer?.OnUnPossessionState(this);
    }

    private ILeverControls GetLeverUnderCursor()
    {
        if (_playerCamera == null || Mouse.current == null) return null;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 viewportPosition = new(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        Ray ray = _playerCamera.ViewportPointToRay(viewportPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, leverRaycastDistance)) return null;

        LeverPullStation lever = hit.collider.GetComponentInParent<LeverPullStation>();
        return lever != null && !lever.isLocked ? lever : null;
    }
}
