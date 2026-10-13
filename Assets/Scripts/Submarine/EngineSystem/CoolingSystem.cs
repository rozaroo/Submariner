using System;
using System.Collections;
using UnityEngine;

public class CoolingSystem : MonoBehaviour
{
    [SerializeField] private EngineSystem engineSystem;

    [SerializeField] private float coolingInterval = 10f;
    [SerializeField] private float maxUsageTime = 30f;
    [SerializeField] private float cooldownTime = 20f;
    [SerializeField] private float energyConsumption = 5f;

    private CoolingState _state;
    private Coroutine _coolingRoutine;
    [SerializeField] private LeverPullStation coolingLeverPull;

    private bool _hasRegisteredCoolingConsumption;
    private bool _isBlackout;

    private void OnEnable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += OnEnergyStatusChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
    }

    private void Start()
    {
        if (coolingLeverPull != null)
        {
            coolingLeverPull.onActivation += StartCooling;
            coolingLeverPull.onDeactivation += StopCooling;
        }

        if (engineSystem != null)
        {
            engineSystem.OnEngineStartedEvent += CheckCoolingState;
            engineSystem.OnEngineStoppedEvent += StopCooling;
        }
    }

    private void OnDestroy()
    {
        if (coolingLeverPull != null)
        {
            coolingLeverPull.onActivation -= StartCooling;
            coolingLeverPull.onDeactivation -= StopCooling;
        }

        if (engineSystem != null)
        {
            engineSystem.OnEngineStartedEvent -= CheckCoolingState;
            engineSystem.OnEngineStoppedEvent -= StopCooling;
        }
    }

    private void OnEnergyStatusChanged(OnEnergyStatusChange status)
    {
        _isBlackout = (status.energyStatus == EnergyStatus.Empty);
        if (_isBlackout && _state == CoolingState.Active)
        {
            StopCooling();
            if (coolingLeverPull != null)
            {
                coolingLeverPull.SetActive(false);
            }
        }
    }

    private void CheckCoolingState()
    {
        if (!_isBlackout && coolingLeverPull != null && coolingLeverPull.isActive)
        {
            StartCooling();
        }
    }

    public void StartCooling()
    {
        Log.Info("Cooling Lever Activated.");
        if (_isBlackout)
        {
            Log.Info("Cooling blocked. No energy.");
            if (coolingLeverPull != null)
            {
                coolingLeverPull.SetActive(false);
            }
            return;
        }

        if (_state != CoolingState.Off)
        {
            Log.Info("Cooling already active.");
            return;
        }

        if (!engineSystem.IsRunning())
        {
            Log.Info("Cooling blocked. Engine not running.");
            return;
        }

        _state = CoolingState.Active;
        AddEnergyConsumption();
        _coolingRoutine = StartCoroutine(CoolingRoutine());
        Log.Info("Cooling Started.");
    }

    public void StopCooling()
    {
        Log.Info("Cooling Stopped.");
        if (_state != CoolingState.Active) return;

        if (_coolingRoutine != null)
        {
            StopCoroutine(_coolingRoutine);
            _coolingRoutine = null;
        }

        RemoveEnergyConsumption();
        _state = CoolingState.Off;
    }

    private IEnumerator CoolingRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < maxUsageTime)
        {
            if (_isBlackout || !engineSystem.IsRunning())
            {
                StopCooling();
                yield break;
            }
            Log.Info($"Cooling Tick | Elapsed: {elapsedTime}/{maxUsageTime}");
            yield return new WaitForSeconds(coolingInterval);
            if (_isBlackout || !engineSystem.IsRunning())
            {
                StopCooling();
                yield break;
            }
            engineSystem.CoolEngine();
            elapsedTime += coolingInterval;
        }
        Log.Info("Cooling applied.");
        EnterCooldown();
    }

    private void EnterCooldown()
    {
        Log.Info($"Cooling entered cooldown ({cooldownTime}s)");
        RemoveEnergyConsumption();
        _state = CoolingState.Cooldown;
        if (_coolingRoutine != null)
        {
            StopCoroutine(_coolingRoutine);
            _coolingRoutine = null;
        }
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        Log.Info("Cooling System Cooldown");
        yield return new WaitForSeconds(cooldownTime);
        _state = CoolingState.Off;
        Log.Info("Cooling cooldown finished.");
    }

    private void AddEnergyConsumption()
    {
        if (_hasRegisteredCoolingConsumption)
        {
            return;
        }

        _hasRegisteredCoolingConsumption = true;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, true));
        Log.Info($"Cooling consumption registered: {energyConsumption}");
    }

    private void RemoveEnergyConsumption()
    {
        if (!_hasRegisteredCoolingConsumption)
        {
            return;
        }

        _hasRegisteredCoolingConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Log.Info($"Cooling consumption relieved: {energyConsumption}");
    }
}