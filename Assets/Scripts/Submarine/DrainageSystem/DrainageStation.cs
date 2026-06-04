using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class DrainageStation : MonoBehaviour, IPossessable, IInteractable
{
    [Header("References")]
    [SerializeField] private LeverStation mainLever;
    
    [Header("Visual Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    
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

    [Header("Event Channels")]
    [SerializeField] private EnergyStatusEventSO onEnergyStatusChange;
    [SerializeField] private EnergyToConsumeEventChannelSO onEnergyToConsume;
    [SerializeField] private DrainagePropertyEventChannelSO onDrainageStatusChanged;

    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private DrainageMinigame _minigame;
    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private ILeverControls _currentDraggedControls;
    private Vector2 _mouseDelta;
    private bool _isDrainageActive;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;

    #region Initialization

    private void Awake()
    {
        _minigame = GetComponent<DrainageMinigame>();
    }
    
    private void OnEnable()
    {
        onEnergyStatusChange.OnEventRaised += OnEnergyStatusChanged;
        if (_minigame != null)
        {
            _minigame.FinishedMiniGame += OnUnlockLever;
        }
        else
        {
            Log.Warning("[DrainageStation] Minigame Not Set");
        }

        if (mainLever != null)
        {
            mainLever.onActivation += OnLeverActivationSequence;
            mainLever.onDeactivation += OnLeverDeactivationSequence;
        }
        else
        {
            Log.Warning("[DrainageStation] Main Lever Not Set");
        }
    }

    private void OnDisable()
    {
        onEnergyStatusChange.OnEventRaised -= OnEnergyStatusChanged;
        if (_minigame != null)
        {
            _minigame.FinishedMiniGame -= OnUnlockLever;
        }
        
        if (mainLever != null)
        {
            mainLever.onActivation -= OnLeverActivationSequence;
            mainLever.onDeactivation -= OnLeverDeactivationSequence;
        }
    }

    #endregion
    
    private void Update()
    {
        HandleControlDragging();
    }

    public void Interact(PlayerCharacter player)
    {
        player.OnPossessionState(this, true);
    }

    #region PosessionLogic

    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _playerCamera = player.camController.MainCamera;
        
        var clickAction = _currentPlayer.input.actions[clickActionName];
        var exitAction = _currentPlayer.input.actions[exitActionName];
        
        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
        exitAction.started += OnExitPerformed;
    
        CheckDrainageMinigame();
        enabled = true;
    }

    public void UnPossess()
    {
        var clickAction = _currentPlayer.input.actions[clickActionName];
        var exitAction = _currentPlayer.input.actions[exitActionName];
                
        clickAction.started -= OnClickStarted;
        clickAction.canceled -= OnClickCanceled;
        exitAction.started -= OnExitPerformed;

        _currentDraggedControls = null;
        _currentPlayer = null;
        _playerCamera = null;
        enabled = false;
    }

    #endregion

    #region InputActions

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _playerCamera.ScreenPointToRay(mousePos);

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
        _currentPlayer.OnUnPossessionState();
    }
    
    private void HandleControlDragging()
    {
        if (_currentDraggedControls == null || Mouse.current == null) return;
        _mouseDelta = _currentPlayer.input.actions[pointerDeltaActionName].ReadValue<Vector2>();
        float mouseDeltaY = _mouseDelta.y;
        _currentDraggedControls.OnActionDrag(mouseDeltaY);
    }
    
    #endregion
    
    #region Lever Logic

    private void OnUnlockLever()
    {
        mainLever.Unlock();
    }

    private void OnLeverActivationSequence()
    {
        StartDrainage();
    }

    private void OnLeverDeactivationSequence()
    {
        StopDrainage();
        mainLever.Restart();
        _minigame.SetupMiniGame();
    }

    #endregion
    
    #region DrainageLogic
    
    private void StartDrainage()
    {
        if (_energyStatus == EnergyStatus.Empty)
        {
            Log.Info("Not Enough Energy to start the drainage");
            _currentPlayer.OnUnPossessionState();
            return;
        }
        if (!_isDrainageActive)
        {
            onDrainageStatusChanged.RaiseEvent(CreateDrainageProperty());
            _isDrainageActive = true;
            HandleDrainageEnergy();
            Log.Info("Drainage Active");
        }
        _currentPlayer.OnUnPossessionState();
    }
    
    private void StopDrainage()
    {
        _isDrainageActive = false;
        HandleDrainageEnergy();
        onDrainageStatusChanged.RaiseEvent(new DrainagePropertyData
        {
            drainagePercentage = 0f
        });
    }
    
    private void CheckDrainageMinigame()
    {
        if (!_isDrainageActive && !_minigame.hasSucceeded)
        {
            _minigame.SetupMiniGame();
        }
    }
    
    private DrainagePropertyData CreateDrainageProperty()
    {
        return new DrainagePropertyData
        {
            drainagePercentage = drainagePercentage
        };
    }
    
    private void SetDrainageStatus()
    {
        switch (_energyStatus)
        {
            case EnergyStatus.Full:
                drainagePercentage = 1f;
                break;
            case EnergyStatus.Low:
                drainagePercentage = 0.5f;
                break;
            case EnergyStatus.Empty:
                StopDrainage();
                break; 
        }
    }
    
    #endregion

    #region Energy Logic

    private void OnEnergyStatusChanged(EnergyStatus status)
    {
        _energyStatus = status;
        SetDrainageStatus();
    }
    
    private void HandleDrainageEnergy()
    {
        if (_isDrainageActive)
        {
            onEnergyToConsume?.RaiseEvent(new EnergyConsumeData
            {
                energyToConsumeRate = energyConsumption,
                isAddingStress = true
            });
        }
        else
        {
            onEnergyToConsume?.RaiseEvent(new EnergyConsumeData
            {
                energyToConsumeRate = energyConsumption,
                isAddingStress = false
            });
        }
    }

    #endregion
}