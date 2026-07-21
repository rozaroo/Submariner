using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class DrainageStation : MonoBehaviour, IPossessable, IInteractable
{
    [Header("References")]
    [SerializeField] private LeverPullStation mainLeverPull;
    
    [Header("Possession Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private CursorLockMode cursorLockMode;
    [SerializeField] private bool showMouseCursor;
    
    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;
    
    [Header("Input Settings")]
    [SerializeField] private string clickActionName;
    [SerializeField] private string pointerDeltaActionName;
    [SerializeField] private string exitActionName;

    [Header("Drainage Settings")] 
    [SerializeField] private float drainagePercentage = 1f;

    [Header("Energy Consumption Settings")] 
    [SerializeField] private float energyConsumption = 5;

    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private DrainageMinigame _minigame;
    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private ILeverControls _currentDraggedControls;
    private Vector2 _mouseDelta;
    private bool _isDrainageActive;
    private bool _hasRegisteredDrainageConsumption;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    #region Initialization

    private void Awake()
    {
        _minigame = GetComponent<DrainageMinigame>();
    }
    
    private void OnEnable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += OnEnergyStatusChanged;
        
        if (_minigame != null)
        {
            _minigame.FinishedMiniGame += OnUnlockLever;
        }
        else
        {
            Log.Warning("[DrainageStation] Minigame Not Set");
        }

        if (mainLeverPull != null)
        {
            mainLeverPull.onActivation += OnLeverActivationSequence;
            mainLeverPull.onDeactivation += OnLeverDeactivationSequence;
        }
        else
        {
            Log.Warning("[DrainageStation] Main Lever Not Set");
        }
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
        
        if (_minigame != null)
        {
            _minigame.FinishedMiniGame -= OnUnlockLever;
        }
        
        if (mainLeverPull != null)
        {
            mainLeverPull.onActivation -= OnLeverActivationSequence;
            mainLeverPull.onDeactivation -= OnLeverDeactivationSequence;
        }
    }

    #endregion
    
    private void Update()
    {
        HandleControlDragging();
    }

    public void Interact(PlayerCharacter player)
    {
        player.OnPossessionState(this);
    }

    #region PosessionLogic

    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _playerCamera = player.CamController.MainCamera;
        
        InputAction clickAction = _currentPlayer.Input.actions[clickActionName];
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];
        
        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
        exitAction.started += OnExitPerformed;
    
        CheckDrainageMinigame();
        enabled = true;
    }

    public void UnPossess()
    {
        InputAction clickAction = _currentPlayer.Input.actions[clickActionName];
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];
                
        clickAction.started -= OnClickStarted;
        clickAction.canceled -= OnClickCanceled;
        exitAction.started -= OnExitPerformed;

        _currentDraggedControls = null;
        _currentPlayer = null;
        _playerCamera = null;
    }

    #endregion

    #region InputActions

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (Mouse.current == null) return;
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 viewportPos = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
        Ray ray = _playerCamera.ViewportPointToRay(viewportPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, 5f)) return;
    
        if (hit.collider.TryGetComponent(out IButtonControls buttonControl))
        {
            buttonControl.OnActionDown();
        }
    
        if (hit.collider.TryGetComponent(out ILeverControls leverControl))
        {
            _currentDraggedControls = leverControl;
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        _currentDraggedControls = null; 
    }
    
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        if (!_isDrainageActive)
        {
            _minigame.RestartMinigame();
        }
        _currentPlayer.OnUnPossessionState(this);
    }
    
    private void HandleControlDragging()
    {
        if (_currentDraggedControls == null || Mouse.current == null) return;
        _mouseDelta = _currentPlayer.Input.actions[pointerDeltaActionName].ReadValue<Vector2>();
        float mouseDeltaY = _mouseDelta.y;
        _currentDraggedControls.OnActionDrag(mouseDeltaY);
    }
    
    #endregion
    
    #region Lever Logic

    private void OnUnlockLever()
    {
        mainLeverPull.Unlock();
    }

    private void OnLeverActivationSequence()
    {
        SFXManager.PostEvent("Start_DrainagePumpSFX", gameObject);
        StartDrainage();
    }

    private void OnLeverDeactivationSequence()
    {
        SFXManager.PostEvent("Stop_DrainagePumpSFX", gameObject);
        StopDrainage();
        mainLeverPull.Restart();
        _minigame.SetupMiniGame();
    }

    #endregion
    
    #region DrainageLogic
    
    private void StartDrainage()
    {
        if (_energyStatus == EnergyStatus.Empty)
        {
            Log.Info("Not Enough Energy to start the drainage");
            _currentPlayer.OnUnPossessionState(this);
            return;
        }
        if (!_isDrainageActive)
        {
            GameEventChannel<OnDrainagePropertyChange>.RaiseEvent(new OnDrainagePropertyChange(drainagePercentage));
            _isDrainageActive = true;
            StartDrainageEnergyConsumption();
            Log.Info("Drainage Active");
        }
        _currentPlayer.OnUnPossessionState(this);
    }
    
    private void StopDrainage()
    {
        if (!_isDrainageActive && !_hasRegisteredDrainageConsumption)
        {
            GameEventChannel<OnDrainagePropertyChange>.RaiseEvent(new OnDrainagePropertyChange(0f));
            return;
        }

        _isDrainageActive = false;
        StopDrainageEnergyConsumption();
        GameEventChannel<OnDrainagePropertyChange>.RaiseEvent(new OnDrainagePropertyChange(0f));
        Log.Info("Drainage Stopped");
    }
    
    private void CheckDrainageMinigame()
    {
        if (!_isDrainageActive && !_minigame.hasSucceeded)
        {
            _minigame.SetupMiniGame();
        }
    }
    
    private void SetDrainageStatus()
    {
        switch (_energyStatus)
        {
            case EnergyStatus.Full: drainagePercentage = 1f; break;
            case EnergyStatus.Low: drainagePercentage = 0.5f; break;
            case EnergyStatus.Empty: drainagePercentage = 0f; break; 
        }

        if (_isDrainageActive)
        {
            GameEventChannel<OnDrainagePropertyChange>.RaiseEvent(new OnDrainagePropertyChange(drainagePercentage));
        }
    }
    
    #endregion

    #region Energy Logic

    private void OnEnergyStatusChanged(OnEnergyStatusChange status)
    {
        _energyStatus = status.energyStatus;
        
        if (_energyStatus == EnergyStatus.Empty && _isDrainageActive)
        {
            if (mainLeverPull != null)
            {
                mainLeverPull.SetActive(false);
            }
        }
        
        SetDrainageStatus();
    }
    
    private void StartDrainageEnergyConsumption()
    {
        if (_hasRegisteredDrainageConsumption)
        {
            return;
        }

        _hasRegisteredDrainageConsumption = true;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, true));
        Log.Info($"Drainage consumption registered: {energyConsumption}");
    }

    private void StopDrainageEnergyConsumption()
    {
        if (!_hasRegisteredDrainageConsumption)
        {
            return;
        }

        _hasRegisteredDrainageConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Log.Info($"Drainage consumption relieved: {energyConsumption}");
    }

    #endregion
}
