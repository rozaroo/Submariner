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
    
    [Header("Knockback From Submarine Impact")]
    [SerializeField] private float minImpactSpeed = 1f;
    [SerializeField] private float maxExpectedImpactSpeed = 15f;
    [SerializeField] private float minForce = 2f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private float minDrag = 0.5f;
    [SerializeField] private float maxDrag = 2f;
    [SerializeField] private float minAccelTime = 0.5f;
    [SerializeField] private float maxAccelTime = 1.5f;

    private bool _canMove = true;
    private bool _isHolding;
    private CharacterController _controller;

    private InputAction _interactionAction;
    private InputAction _dropAction;
    private InputAction _useAction;

    private StateMachine _gameplaySm;

    private IMovementStrategy _movementStrategy;
    private MovementContext _movementContext;
    
    private Vector3 _defaultCameraLocalPosition;
    private Quaternion _defaultCameraLocalRotation;

    public CameraPose SavedCameraPose { get; private set; }
    public PlayerInput Input { get; private set; }
    public CameraController CamController { get; private set; }
    public InventorySystem InventorySystem { get; private set; }
    public FootstepSystem FootstepSystem { get; private set; }
    public Vector3 DefaultCameraLocalPosition => _defaultCameraLocalPosition;
    public Quaternion DefaultCameraLocalRotation => _defaultCameraLocalRotation;
    public CharacterController CharacterController => _controller;
    
    private void OnEnable()
    {
        GameEventChannel<OnPlayerInputStateChanged>.OnEventRaised += HandleInputState;
        GameEventChannel<OnSubmarineImpact>.OnEventRaised += OnSubmarineImpact;
    }

    private void OnDisable()
    {
        GameEventChannel<OnPlayerInputStateChanged>.OnEventRaised -= HandleInputState;
        GameEventChannel<OnSubmarineImpact>.OnEventRaised -= OnSubmarineImpact;
    }
    
    private void Start()
    {
        Input = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        CamController = GetComponent<CameraController>();
        InventorySystem = GetComponent<InventorySystem>();
        FootstepSystem = GetComponent<FootstepSystem>();
        
        if (CamController != null)
        {
            _defaultCameraLocalPosition = CamController.MainCamera.transform.localPosition;
            _defaultCameraLocalRotation = CamController.MainCamera.transform.localRotation;
        }

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
    
    private void HandleInputState(OnPlayerInputStateChanged state)
    {
        _canMove = state.IsInputEnabled;
        
        if (!_canMove && TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (!_canMove) return;
        _gameplaySm.Update();
        _movementStrategy?.Move(_movementContext);
        
        if (_isHolding)
        {
            InventorySystem.UseItemHold();
        }
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
            new PlayerGameplayPossessionState(_gameplaySm, this, station, Input.currentActionMap.name));
    }

    public void OnUnPossessionState(IPossessable station)
    {
        string prevMap = "Player";
        if (_gameplaySm.CurrentState is PlayerGameplayPossessionState possessionState)
        {
            prevMap = possessionState.PreviousMapName;
        }
        _gameplaySm.ChangeState(new PlayerGameplayUnPossessionState(_gameplaySm, this, station, prevMap));
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
        if (!_isHolding) return;
        _isHolding = false;
        InventorySystem.UseItemReleased();
    }

    #endregion

    #region Events

    private void OnSubmarineImpact(OnSubmarineImpact data)
    {
        if (data.ImpactSpeed < minImpactSpeed) return;
        
        if (_gameplaySm.CurrentState is PlayerGameplayPossessionState ||
            _gameplaySm.CurrentState is PlayerGameplayUnPossessionState)
            return;

        SetGameplayStateFromImpact(data.Normal, data.ImpactSpeed);
    }
    
    private void SetGameplayStateFromImpact(Vector3 impactNormal, float impactSpeed)
    {
        if (_gameplaySm == null) return;
        
        float t = Mathf.Clamp01(impactSpeed / maxExpectedImpactSpeed);
        
        Vector3 direction = impactNormal;
        direction.y = 0;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : -transform.forward;

        float force = Mathf.Lerp(minForce, maxForce, t);
        float drag = Mathf.Lerp(maxDrag, minDrag, t);
        float accelerationTime = Mathf.Lerp(minAccelTime, maxAccelTime, t);

        _gameplaySm.ChangeState(new PlayerGameplayExplosionState(_gameplaySm, this, direction, force, drag, accelerationTime));
    }

    #endregion

    #region Testing

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

    #endregion
}