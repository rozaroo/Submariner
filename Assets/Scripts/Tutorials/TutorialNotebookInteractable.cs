using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class TutorialNotebookInteractable : MonoBehaviour
{
    [Header("Notebook")]
    [SerializeField] private TutorialNotebook notebook;

    [Header("Input")]
    [SerializeField] private string clickActionName = "ClickInteraction";
    [SerializeField] private string exitActionName = "ExitStation";

    [Header("Raycast")]
    [SerializeField] private float raycastDistance = 10f;

    private PlayerCharacter _player;
    private Camera _playerCamera;

    private InputAction _clickAction;
    private InputAction _exitAction;

    private bool _isActive;

    private void Awake()
    {
        if (notebook == null)
            notebook = GetComponent<TutorialNotebook>();
    }

    public void Activate(PlayerCharacter player)
    {
        if (player == null)
            return;

        _player = player;
        _playerCamera = player.CamController.MainCamera;

        if (_playerCamera == null)
        {
            Debug.LogWarning(
                $"{nameof(TutorialNotebookInteractable)}: Player camera not found.",
                this
            );

            return;
        }

        if (_player.Input == null)
        {
            Debug.LogWarning(
                $"{nameof(TutorialNotebookInteractable)}: Player Input not found.",
                this
            );

            return;
        }

        _clickAction = _player.Input.actions[clickActionName];
        _exitAction = _player.Input.actions[exitActionName];

        if (_clickAction != null)
        {
            _clickAction.started += OnClickStarted;
        }

        if (_exitAction != null)
        {
            _exitAction.started += OnExitStarted;
        }

        _isActive = true;
    }


    public void Deactivate()
    {
        if (_clickAction != null)
            _clickAction.started -= OnClickStarted;

        if (_exitAction != null)
            _exitAction.started -= OnExitStarted;

        if (notebook != null)
            notebook.CancelPresentation();

        _clickAction = null;
        _exitAction = null;

        _playerCamera = null;
        _player = null;

        _isActive = false;
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (!_isActive || notebook == null || _playerCamera == null)
            return;

        Ray ray = _playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                raycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (!notebook.OwnsCollider(hit.collider))
            return;

        if (notebook.IsReading)
        {
            notebook.Close();
        }
        else
        {
            notebook.Open(_playerCamera);
        }
    }

    private void OnExitStarted(InputAction.CallbackContext context)
    {
        if (!_isActive || notebook == null)
            return;

        if (notebook.IsReading)
        {
            notebook.Close();
        }
    }

    private void Update()
    {
        if (!_isActive || notebook == null || _playerCamera == null)
            return;

        notebook.SetHovered(IsNotebookUnderCursor());
    }

    private bool IsNotebookUnderCursor()
    {
        if (Mouse.current == null)
            return false;

        Ray ray = _playerCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                raycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return notebook.OwnsCollider(hit.collider);
    }

    private void OnDisable()
    {
        Deactivate();
    }
}