using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class DrainageStation : MonoBehaviour, IPossessable
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
    [SerializeField] private string stationMapName;
    
    [Header("Input Settings")]
    [SerializeField] private string exitActionName;

    [Header("Drainage Settings")] 
    [SerializeField] private float drainagePercentage = 1f;

    [Header("Energy Consumption Settings")] 
    [SerializeField] private float energyConsumption = 5;

    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private DrainageMinigame _minigame;
    private PlayerCharacter _currentPlayer;
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
        if (_minigame != null) _minigame.FinishedMiniGame += OnUnlockLever;
        else Log.Warning("[DrainageStation] Minigame Not Set");
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
        if (_minigame != null) _minigame.FinishedMiniGame -= OnUnlockLever;
    }
    #endregion

    #region PosessionLogic

    public void Possess(PlayerCharacter player)
    {
        _currentPlayer = player;
        _currentPlayer.OnPossessionState(this);
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];
        exitAction.started += OnExitPerformed;
        CheckDrainageMinigame();
        enabled = true;
    }

    public void UnPossess()
    {
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];
        exitAction.started -= OnExitPerformed;
        _currentPlayer.OnUnPossessionState(this);
        _currentPlayer = null;
        enabled = false;
    }
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        UnPossess();
    }

    #endregion

    #region Lever Logic

    private void OnUnlockLever()
    {
        mainLeverPull.Unlock();
    }

    public void OnLeverActivationSequence()
    {
        Debug.Log("Drainage Activated");
        SFXManager.PostEvent("Start_DrainagePumpSFX", gameObject);
        StartDrainage();
    }

    public void OnLeverDeactivationSequence()
    {
        Debug.Log("Drainage Deactivated");
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
            Debug.Log("Not Enough Energy to start the drainage");
            return;
        }
        if (!_isDrainageActive)
        {
            GameEventChannel<OnDrainagePropertyChange>.RaiseEvent(new OnDrainagePropertyChange(drainagePercentage));
            _isDrainageActive = true;
            StartDrainageEnergyConsumption();
            Debug.Log("Drainage Active");
        }
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
        Debug.Log("Drainage Stopped");
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
        Debug.Log($"Drainage consumption registered: {energyConsumption}");
    }

    private void StopDrainageEnergyConsumption()
    {
        if (!_hasRegisteredDrainageConsumption)
        {
            return;
        }

        _hasRegisteredDrainageConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Debug.Log($"Drainage consumption relieved: {energyConsumption}");
    }

    #endregion
}
