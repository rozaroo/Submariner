using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGameplayFreeState : IState
{
    public PlayerGameplayContext Context { get; set; }
    private bool _isHolding;
    private InputAction _interactionAction;
    private InputAction _dropAction;
    private InputAction _useAction;

    public PlayerGameplayFreeState(PlayerGameplayContext context)
    {
        Context = context;
    }

    public void OnEnter()
    {
        _isHolding = false;
        
        _interactionAction = Context.Input.actions[Context.InteractionActionName];
        _dropAction = Context.Input.actions[Context.DropActionName];
        _useAction = Context.Input.actions[Context.UseActionName];

        _interactionAction.started += TryInteractRaycast;
        _dropAction.started += TryDropItem;
        _useAction.started += OnUseStarted;
        _useAction.performed += TryUseItem;
        _useAction.canceled += OnUseReleased;
        
        Context.Player.playerMovementSm.ChangeState(
            Context.Player.movementState);
    }

    public void Update()
    {
        if (_isHolding)
            Context.InventorySystem.UseItemHold();
    }

    public void LateUpdate() { }

    public void OnExit()
    {
        _interactionAction.started -= TryInteractRaycast;
        _dropAction.started -= TryDropItem;
        _useAction.started -= OnUseStarted;
        _useAction.performed -= TryUseItem;
        _useAction.canceled -= OnUseReleased;
        
        //_isHolding = false;
    }

    private void TryInteractRaycast(InputAction.CallbackContext ctx)
    {
        Ray ray = new Ray(
            Context.MainCamera.transform.position,
            Context.MainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 
                Context.InteractionDistance, Context.InteractableLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 2f);
            if (hit.collider.TryGetComponent(out IInteractable interactable))
                interactable.Interact(Context.Player);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * Context.InteractionDistance, Color.red, 2f);
        }
    }

    private void TryDropItem(InputAction.CallbackContext ctx)
        => Context.InventorySystem.DropItem();

    private void OnUseStarted(InputAction.CallbackContext ctx)
        => _isHolding = false;

    private void TryUseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
            _isHolding = true;
        else
        {
            _isHolding = false;
            Context.InventorySystem.UseItem();
        }
    }

    private void OnUseReleased(InputAction.CallbackContext ctx)
    {
        if (!_isHolding) return;
        _isHolding = false;
        Context.InventorySystem.UseItemReleased();
    }
}