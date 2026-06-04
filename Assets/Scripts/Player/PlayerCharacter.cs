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

    private CharacterController _controller;

    private StateMachine _gameplaySm;

    private IState _lockedMovementState;

    private PlayerMovementContext _playerMovementContext;
    private PlayerGameplayContext _gameplayContext;
    
    public StateMachine playerMovementSm { get; private set; }
    public IState movementState { get; private set; }
    public IState lockedMovementState { get; private set; }
    public PlayerInput input { get; private set; }
    public CameraController camController { get; private set; }
    public InventorySystem inventorySystem { get; private set; }

    private void Start()
    {
        input     = GetComponent<PlayerInput>();
        _controller      = GetComponent<CharacterController>();
        camController = GetComponent<CameraController>();
        inventorySystem = GetComponent<InventorySystem>();

        //Movement
        _playerMovementContext = new PlayerMovementContext(
            transform, moveSpeed,
            input.actions.FindAction(moveActionName),
            _controller, input);

        playerMovementSm = new StateMachine();
        movementState = new PlayerMovementState(_playerMovementContext);
        _lockedMovementState = new PlayerLockedMovementState();

        //Gameplay
        _gameplayContext = new PlayerGameplayContext(
            this, camController, inventorySystem, input,
            interactionDistance, interactableLayer,
            interactionActionName, dropActionName, useActionName);

        _gameplaySm = new StateMachine();
        _gameplaySm.ChangeState(new PlayerGameplayFreeState(_gameplayContext));

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        _gameplaySm.Update();
        playerMovementSm.Update();
    }

    private void LateUpdate()
    {
        _gameplaySm.LateUpdate();
        playerMovementSm.LateUpdate();
    }

    #region State Machine

    public void OnPossessionState(IPossessable station, bool needsTransition)
    {
        _gameplaySm.ChangeState(new PlayerGameplayPossessionState(this, station, needsTransition));
    }

    public void OnUnPossessionState()
    {
        _gameplaySm.ChangeState(new PlayerGameplayFreeState(_gameplayContext));
    }

    #endregion
    
}
