using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter : MonoBehaviour
{
    [Header("Control Settings")] 
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string interactionActionName = "Interact";
    [SerializeField] private string dropActionName = "Drop";
    [SerializeField] private string useActionName = "Click";

    [Header("Interaction Settings (Raycast)")] 
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private LayerMask interactableLayer;

    private bool _isHolding;
    private CharacterController _controller;

    private InputAction _interactionAction;
    private InputAction _dropAction;
    private InputAction _useAction;

    private StateMachine _gameplaySm;

    private IMovementStrategy _movementStrategy;
    private MovementContext _movementContext;

    public CameraPose SavedCameraPose { get; private set; }
    public PlayerInput Input { get; private set; }
    public CameraController CamController { get; private set; }
    public InventorySystem InventorySystem { get; private set; }
    public CharacterController CharacterController => _controller;


    private void Start()
    {
        Input = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        CamController = GetComponent<CameraController>();
        InventorySystem = GetComponent<InventorySystem>();

        _movementContext = new MovementContext
        {
            CharacterController = _controller,
            Transform = transform,
            MovementAction = Input.actions[moveActionName],
            MoveSpeed = moveSpeed,
        };

        _gameplaySm = new StateMachine();
        PlayerGameplayState freeState = new PlayerGameplayFreeState(_gameplaySm, this);
        _gameplaySm.SetInitialState(freeState);

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        _gameplaySm.Update();
        _movementStrategy?.Move(_movementContext);
    }

    public void SetMovementStrategy(IMovementStrategy movementStrategy)
    {
        _movementStrategy = movementStrategy;
    }

    #region State Machine

    public void OnPossessionState(IPossessable station)
    {
        SavedCameraPose = new CameraPose(CamController.MainCamera.transform.position,
            CamController.MainCamera.transform.rotation);
        _gameplaySm.ChangeState(
            new PlayerGameplayPossessionState(_gameplaySm, this, station, Input.currentActionMap.name,
                station.CursorLockMode, station.IsMouseVisible));
    }

    public void OnUnPossessionState(IPossessable station)
    {
        _gameplaySm.ChangeState(new PlayerGameplayUnPossessionState(_gameplaySm, this, station));
    }

    #endregion

    #region Inputs

    public void EnableGameplayInputs()
    {
        _interactionAction =
            Input.actions[interactionActionName];

        _dropAction =
            Input.actions[dropActionName];

        _useAction =
            Input.actions[useActionName];

        _interactionAction.started += TryInteractRaycast;

        _dropAction.started += TryDropItem;

        _useAction.started += OnUseStarted;
        _useAction.performed += TryUseItem;
        _useAction.canceled += OnUseReleased;
    }

    public void DisableGameplayInputs()
    {
        if (_interactionAction == null) return;
        
        _interactionAction.started -= TryInteractRaycast;

        _dropAction.started -= TryDropItem;

        _useAction.started -= OnUseStarted;
        _useAction.performed -= TryUseItem;
        _useAction.canceled -= OnUseReleased;
    }

    public void SetMouseConfiguration(CursorLockMode cursorLockMode, bool isMouseVisible)
    {
        Cursor.lockState = cursorLockMode;
        Cursor.visible = isMouseVisible;
    }

    private void TryInteractRaycast(InputAction.CallbackContext ctx)
    {
        Ray ray = new Ray(
            CamController.MainCamera.transform.position,
            CamController.MainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 2f);
            if (hit.collider.TryGetComponent(out IInteractable interactable))
                interactable.Interact(this);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 2f);
        }
    }

    private void TryDropItem(InputAction.CallbackContext ctx) => InventorySystem.DropItem();

    private void OnUseStarted(InputAction.CallbackContext ctx) => _isHolding = false;

    private void TryUseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is HoldInteraction)
            _isHolding = true;
        else
        {
            _isHolding = false;
            InventorySystem.UseItem();
        }
    }

    private void OnUseReleased(InputAction.CallbackContext ctx)
    {
        if (!_isHolding) return;
        _isHolding = false;
        InventorySystem.UseItemReleased();
    }

    #endregion
    
    [ContextMenu("Change to Desired Set Movement Strategy")] //ONLY FOR TESTING
    public void SetMovementStrategy()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        _movementStrategy = new KnockbackMovement(randomDirection, 5f, 1f);
    }

    [ContextMenu("Change to Desired Gameplay State")] //ONLY FOR TESTING
    public void SetGameplayState()
    {
        if (_gameplaySm != null)
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            _gameplaySm.ChangeState(new PlayerGameplayExplosionState(_gameplaySm, this, randomDirection, 5f, 1f, 1f));
        }
    }

    [ContextMenu("Change to Normal Gameplay State")] //ONLY FOR TESTING
    public void ReturnToNormalState()
    {
        if(_gameplaySm != null)
            _gameplaySm.ChangeState(new PlayerGameplayFreeState(_gameplaySm, this));
    }
}