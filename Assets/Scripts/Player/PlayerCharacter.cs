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
    public CameraController camController { get; private set; }
    public InventorySystem inventorySystem { get; private set; }
    

    private void Start()
    {
        Input = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        camController = GetComponent<CameraController>();
        inventorySystem = GetComponent<InventorySystem>();
        
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
        _movementStrategy.Move(_movementContext);
    }
    
    public void SetMovementStrategy(IMovementStrategy movementStrategy)
    {
        _movementStrategy = movementStrategy;
    }

    #region State Machine

    public void OnPossessionState(IPossessable station)
    {
        SavedCameraPose = new CameraPose(camController.MainCamera.transform.position, camController.MainCamera.transform.rotation);
        _gameplaySm.ChangeState(
            new PlayerGameplayPossessionState(_gameplaySm, this, station, Input.currentActionMap.name));
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
            camController.MainCamera.transform.position,
            camController.MainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 
                interactionDistance, interactableLayer))
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

    private void TryDropItem(InputAction.CallbackContext ctx)
        => inventorySystem.DropItem();

    private void OnUseStarted(InputAction.CallbackContext ctx)
        => _isHolding = false;

    private void TryUseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is HoldInteraction)
            _isHolding = true;
        else
        {
            _isHolding = false;
            inventorySystem.UseItem();
        }
    }

    private void OnUseReleased(InputAction.CallbackContext ctx)
    {
        if (!_isHolding) return;
        _isHolding = false;
        inventorySystem.UseItemReleased();
    }

    #endregion
}
