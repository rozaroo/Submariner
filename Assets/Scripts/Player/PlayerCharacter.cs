using UnityEngine;
using UnityEngine.InputSystem;

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
    
    private PlayerInput _playerInput;
    private InputAction _movementAction;
    
    private CharacterController _controller;
    private CameraController _cameraController;
    private InventorySystem _inventorySystem;
    private bool _isHolding = false;

    private StateMachine _stateMachine;
    private IState _movementState;
    
    public PlayerInput input => _playerInput;
    public CameraController camController => _cameraController;
    public InventorySystem inventorySystem => _inventorySystem;
    
    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            _movementAction = _playerInput.actions.FindAction(moveActionName);
        }
        _controller = GetComponent<CharacterController>();
        _cameraController = GetComponent<CameraController>();
        _inventorySystem = GetComponent<InventorySystem>();

        PlayerMovementContext playerMovementContext = new PlayerMovementContext(
            transform,
            moveSpeed,
            _movementAction,
            _controller,
            _playerInput
        );

        _stateMachine = new StateMachine();
        _movementState = new PlayerMovementState(playerMovementContext);
        
        _stateMachine.ChangeState(_movementState);
        
        Cursor.lockState = CursorLockMode.Locked;
        
        var interactionAction = _playerInput.actions[interactionActionName];
        interactionAction.started += TryInteractRaycast;
        
        var dropAction = _playerInput.actions[dropActionName];
        dropAction.started += TryDropItem;
        
        var useAction = _playerInput.actions[useActionName];
        useAction.performed += TryUseItem;
        useAction.started   += OnUseStarted;
        useAction.canceled  += OnUseReleased;
    }

    private void Update()
    {
        _stateMachine.Update();
        if (_isHolding)
            inventorySystem.UseItemHold();
    }
    
    private void TryInteractRaycast(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(_cameraController.MainCamera.transform.position, _cameraController.MainCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 2f);
            
            if (hit.collider.TryGetComponent(out IInteractable interactableObject))
            {
                interactableObject.Interact(this);
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 2f);
        }
    }
    
    private void TryDropItem(InputAction.CallbackContext context)
    {
        inventorySystem.DropItem();
    }
    
    private void OnUseStarted(InputAction.CallbackContext ctx)
    {
        _isHolding = false; // reset
    }

    private void TryUseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
        {
            _isHolding = true;
        }
        else
        {
            _isHolding = false;
            inventorySystem.UseItem();
        }
    }

    private void OnUseReleased(InputAction.CallbackContext ctx)
    {
        if (_isHolding)
        {
            _isHolding = false;
            inventorySystem.UseItemReleased();
        }
    }
}
