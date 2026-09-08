using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The engine repair station. It deliberately follows the same possession
/// contract as drainage and the fuse workbench: interact, move the camera to
/// the station, show the cursor, and use the Station input map.
/// </summary>
public class EngineMiniGameActivator : MonoBehaviour, IPossessable, IInteractable
{
    [Header("References")]
    [SerializeField] private EngineMiniGame engineMiniGame;

    [Header("Possession Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.4f;
    [SerializeField] private CursorLockMode cursorLockMode = CursorLockMode.None;
    [SerializeField] private bool showMouseCursor = true;

    [Header("Actions Maps Settings")]
    [SerializeField] private string stationMapName = "Station";
    [SerializeField] private string clickActionName = "ClickInteraction";
    [SerializeField] private string exitActionName = "ExitStation";
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private bool showHoverPreview = false;

    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private InputAction _clickAction;
    private InputAction _exitAction;
    private EngineMiniGameComponent _hoveredComponent;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    private void OnValidate()
    {
        if (cameraAnchor == null || directionAnchor == null)
            Debug.LogWarning("[ENGINE MINIGAME] Asigna Camera Anchor y Direction Anchor para usar el zoom de estación.", this);
    }

    public void Interact(PlayerCharacter player)
    {
        if (player == null) return;
        if (engineMiniGame == null)
        {
            Debug.LogError("[ENGINE MINIGAME] EngineMiniGame no está asignado.");
            return;
        }
        if (!engineMiniGame.CanStart)
        {
            Debug.Log("[ENGINE MINIGAME] El motor no requiere un reinicio de emergencia.");
            return;
        }
        if (cameraAnchor == null || directionAnchor == null)
        {
            Debug.LogError("[ENGINE MINIGAME] Faltan los anchors de cámara de la mesa.");
            return;
        }

        player.OnPossessionState(this);
    }

    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _playerCamera = player.CamController.MainCamera;

        _clickAction = player.Input.actions.FindAction(clickActionName);
        _exitAction = player.Input.actions.FindAction(exitActionName);

        if (_clickAction != null) _clickAction.started += OnClickStarted;
        else Debug.LogError($"[ENGINE MINIGAME] Acción de click '{clickActionName}' no encontrada.");

        if (_exitAction != null) _exitAction.started += OnExitPerformed;
        else Debug.LogError($"[ENGINE MINIGAME] Acción de salida '{exitActionName}' no encontrada.");

        engineMiniGame.Completed += OnMinigameCompleted;
        engineMiniGame.StartMinigame();
        enabled = true;
    }

    public void UnPossess()
    {
        if (_clickAction != null) _clickAction.started -= OnClickStarted;
        if (_exitAction != null) _exitAction.started -= OnExitPerformed;
        if (engineMiniGame != null) engineMiniGame.Completed -= OnMinigameCompleted;

        SetHoveredComponent(null);
        _clickAction = null;
        _exitAction = null;
        _currentPlayer = null;
        _playerCamera = null;
        enabled = false;
    }

    private void Update()
    {
        UpdateHoveredComponent();
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (engineMiniGame == null || !engineMiniGame.IsAcceptingInput) return;

        EngineMiniGameComponent clickedComponent = GetComponentUnderCursor();
        if (clickedComponent != null) clickedComponent.ClickInteract(_currentPlayer);
    }

    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        // Leaving during a repair would leave an unwinnable countdown running.
        // Once it ends (success or failure), ExitStation behaves like the other tables.
        if (engineMiniGame != null && engineMiniGame.IsActive) return;
        _currentPlayer?.OnUnPossessionState(this);
    }

    private void UpdateHoveredComponent()
    {
        if (!showHoverPreview || engineMiniGame == null || !engineMiniGame.IsAcceptingInput)
        {
            SetHoveredComponent(null);
            return;
        }

        SetHoveredComponent(GetComponentUnderCursor());
    }

    private void OnMinigameCompleted()
    {
        _currentPlayer?.OnUnPossessionState(this);
    }

    private EngineMiniGameComponent GetComponentUnderCursor()
    {
        if (_playerCamera == null || Mouse.current == null) return null;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 viewportPosition = new(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        Ray ray = _playerCamera.ViewportPointToRay(viewportPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance)) return null;
        return hit.collider.GetComponentInParent<EngineMiniGameComponent>();
    }

    private void SetHoveredComponent(EngineMiniGameComponent component)
    {
        if (_hoveredComponent == component) return;

        if (_hoveredComponent != null) _hoveredComponent.SetHovered(false);
        _hoveredComponent = component;
        if (_hoveredComponent != null) _hoveredComponent.SetHovered(true);
    }
}
