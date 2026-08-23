using UnityEngine;
using UnityEngine.InputSystem;

public class LeverInteractionStation : MonoBehaviour, IPossessable
{
    [SerializeField]
    private MonoBehaviour controlledLever;

    [Header("Possession Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private CursorLockMode cursorLockMode = CursorLockMode.Locked;
    [SerializeField] private bool showMouseCursor = false;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    [Header("Action Maps")]
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string stationMapName = "Station";

    [Header("Input Actions")]
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string exitActionName = "ExitStation";
    [SerializeField] private string clickActionName = "ClickInteraction";

    private bool _isDragging;

    private PlayerCharacter _currentPlayer;

    private ILeverControls _leverControls;

    private void Awake()
    {
        _leverControls = controlledLever as ILeverControls;
        if (_leverControls == null) Log.Error($"{name}: Controlled Lever must implement ILeverControls.");
        enabled = false;
    }

    #region Possession

    public void Possess(PlayerCharacter player)
    {
        if (_currentPlayer != null) return;
        Log.Info("Lever Possessed");
        cameraAnchor.gameObject.SetActive(true);
        _currentPlayer = player;
        _currentPlayer.OnPossessionState(this);
        var currentMap = _currentPlayer.Input.currentActionMap;
        InputAction lookAction = currentMap.FindAction(lookActionName, true);
        InputAction exitAction = currentMap.FindAction(exitActionName, true);
        InputAction clickAction = currentMap.FindAction(clickActionName, true);
        lookAction.performed += OnLookPerformed;
        exitAction.started += OnExitPerformed;
        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
        enabled = true;
    }

    public void UnPossess()
    {
        if (_currentPlayer == null) return;
        cameraAnchor.gameObject.SetActive(false);
        var currentMap = _currentPlayer.Input.currentActionMap;

        InputAction lookAction = currentMap.FindAction(lookActionName, true);
        InputAction exitAction = currentMap.FindAction(exitActionName, true);
        InputAction clickAction = currentMap.FindAction(clickActionName, true);
        lookAction.performed -= OnLookPerformed;
        exitAction.started -= OnExitPerformed;
        clickAction.started -= OnClickStarted;
        clickAction.canceled -= OnClickCanceled;
        _isDragging = false;
        _currentPlayer.OnUnPossessionState(this);

        _currentPlayer = null;

        enabled = false;

        Log.Info("Lever Released");
    }

    #endregion

    #region Input

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        if (!_isDragging) return;
        if (_leverControls == null) return;
        Vector2 delta = context.ReadValue<Vector2>();
        Log.Info($"[LEVER] Dragging: {delta}");
        _leverControls.OnActionDrag(delta.y);
    }

    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        Log.Info("Exit");
        UnPossess();
    }
    private void OnClickStarted(InputAction.CallbackContext context)
    {
        _isDragging = true;
        Log.Info("[LEVER] Started dragging.");
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        _isDragging = false;
        Log.Info("[LEVER] Stopped dragging.");
    }

    #endregion
}