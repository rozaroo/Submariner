using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private string _moveActionName = "Move";
    [SerializeField] private string _interactionActionName = "Interact";
    [SerializeField] private string _dropActionName = "Drop";
    [SerializeField] private string _useActionName = "Click";
    
    [Header("References Settings")] 
    [SerializeField] CameraController _cameraController;
    private PlayerInput _playerInput;
    private CharacterController _controller;
    
    [Header("Movement Settings")]
    private Vector2 _moveDirectionInput;
    private float _moveVelocityY;
    private float _gravity = -9.81f;
    
    [Header("Interaction Settings (Raycast)")]
    [SerializeField] private Camera playerCamera; //INTERACTION ONLY
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("Inventory")]
    [SerializeField] private InventorySystem _inventorySystem;
    
    private bool _isHolding = false;
    
    public PlayerInput Input => _playerInput;
    public CameraController CamController => _cameraController;
    public CharacterController Controller => _controller;
    public InventorySystem InventorySystem => _inventorySystem;
    
    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        _inventorySystem = GetComponent<InventorySystem>();
        Cursor.lockState = CursorLockMode.Locked;
        
        var interactionAction = _playerInput.actions[_interactionActionName];
        interactionAction.started += TryInteractRaycast;
        
        var dropAction = _playerInput.actions[_dropActionName];
        dropAction.started += TryDropItem;
        
        var useAction = _playerInput.actions[_useActionName];
        useAction.performed += TryUseItem;
        useAction.started   += OnUseStarted;
        useAction.canceled  += OnUseReleased;
    }

    private void Update()
    {
        _moveDirectionInput = _playerInput.actions[_moveActionName].ReadValue<Vector2>();
        Move();

        if (_isHolding)
            InventorySystem.UseItemHold();
    }
    
    private void TryInteractRaycast(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
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
    
    private void Move()
    {
        if (_cameraController.IsTransitioning) return;

        Vector3 move = transform.right * _moveDirectionInput.x + transform.forward * _moveDirectionInput.y;
        if (_controller.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += _gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }

    private void TryDropItem(InputAction.CallbackContext context)
    {
        InventorySystem.DropItem();
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
            InventorySystem.UseItem();
        }
    }

    private void OnUseReleased(InputAction.CallbackContext ctx)
    {
        if (_isHolding)
        {
            _isHolding = false;
            InventorySystem.UseItemReleased();
        }
    }
}
