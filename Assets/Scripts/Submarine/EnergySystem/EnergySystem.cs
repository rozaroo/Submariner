using System;
using System.Collections;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 5000f;
    [SerializeField] private float _currentEnergy;
    [SerializeField] private float energyToRegenPercentage = 1f;
    [SerializeField] private float timeToRegenerateEnergy = 60f;
    
    [Header("Fuse Settings")]
    [SerializeField] private float fuseBreakConsumptionThreshold = 25f;
    [SerializeField] private float energyConsumptionRate = 0f;
    [SerializeField] private float timeTillFuseBreak = 10f;
    [SerializeField] private float startupEnergyPercentageAfterFuseRepair = 10f;
    [SerializeField] private float overloadCheckDelayAfterFuseRepair = 1f;
    [SerializeField] private bool isFuseBroken;

    [Header("Energy Status")]
    [SerializeField] private EnergyStatus _energyStatus;
    private int _stressIndex;
    
    [Header("Coroutines")]
    private Coroutine _energyRegenerationCoroutine;
    private Coroutine _energyConsumptionCoroutine;
    private Coroutine _fuseBreakCoroutine;
    private Coroutine _delayedFuseOverloadCheckCoroutine;
    private float _baseFuseBreakConsumptionThreshold;

    public event Action FuseBurned;
    public event Action FuseRestored;
    public bool IsFuseBroken => isFuseBroken;

    private float CurrentEnergy
    {
        get => _currentEnergy;
        set
        {
            _currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
            GameEventChannel<OnEnergyPropertyChange>.RaiseEvent(new OnEnergyPropertyChange(GetCurrentEnergyPercentage(),100f) );
            SetEnergyStatus();
        }
    }

    #region Initialization

    private void Awake()
    {
        _baseFuseBreakConsumptionThreshold = fuseBreakConsumptionThreshold;
        energyConsumptionRate = 0f;
        _stressIndex = 0;
    }

    private void OnEnable()
    {
        GameEventChannel<OnEnergyConsumption>.OnEventRaised += OnChangeEnergyValues;
    }
    
    private void OnDisable()
    {
        GameEventChannel<OnEnergyConsumption>.OnEventRaised -= OnChangeEnergyValues;
    }
    
    private void Start()
    {
        CurrentEnergy = maxEnergy;
        SetEnergyStatus();
    }

    #endregion

    #region Energy Consumption
    
    [ContextMenu("Consumption/Start Energy Consumption")]
    private void StartEnergyConsumption()
    {
        if (_energyConsumptionCoroutine != null)
        {
            StopCoroutine(_energyConsumptionCoroutine);
        }
        _energyConsumptionCoroutine = StartCoroutine(EnergyConsumption());
    }

    [ContextMenu("Consumption/Stop Energy Consumption")]
    private void StopEnergyConsumption()
    {
        if (_energyConsumptionCoroutine != null)
        {
            StopCoroutine(_energyConsumptionCoroutine);
            _energyConsumptionCoroutine = null;
        }
    }

    private void OnChangeEnergyValues(OnEnergyConsumption consumption)
    {
        if (consumption.isAddingStress)
        {
            energyConsumptionRate += consumption.energyToConsumeRate;
            _stressIndex++;
        }
        else
        {
            energyConsumptionRate -= consumption.energyToConsumeRate;
            _stressIndex = Mathf.Max(0, _stressIndex - 1);
        }

        energyConsumptionRate = Mathf.Max(0f, energyConsumptionRate);

        if (isFuseBroken)
        {
            Log.Info($"Energy Consumption Rate: {Mathf.Abs(energyConsumptionRate)} - Fuse Broken");
            return;
        }

        CheckEnergyConsumption();
        CheckEnergyRegeneration();
        CheckFuseOverload();
    }
    
    private void CheckEnergyConsumption()
    {
        if (energyConsumptionRate == 0)
        {
            StopEnergyConsumption();
        }
        if (energyConsumptionRate > 0)
        {
            StartEnergyConsumption();
        }
        Log.Info($"Energy Consumption Rate: {Mathf.Abs(energyConsumptionRate)} - Stress Index: {_stressIndex}");
    }
    
    private IEnumerator EnergyConsumption()
    {
        while (!isFuseBroken && CurrentEnergy > 0f)
        {
            CurrentEnergy -= energyConsumptionRate * Time.deltaTime;
            yield return null;
        }
        _energyConsumptionCoroutine = null;
    }

    #endregion

    #region Energy Regeneration

    [ContextMenu("Regeneration/Start Energy Regeneration")]
    private void StartEnergyRegeneration()
    {
        if (isFuseBroken)
        {
            return;
        }

        if (_energyRegenerationCoroutine != null)
        {
            StopCoroutine(_energyRegenerationCoroutine);
        }
        _energyRegenerationCoroutine = StartCoroutine(EnergyRegenerateVPercentage());
    }
    
    [ContextMenu("Regeneration/Stop Energy Regeneration")]
    private void StopEnergyRegeneration()
    {
        if (_energyRegenerationCoroutine != null)
        {
            StopCoroutine(_energyRegenerationCoroutine);
            _energyRegenerationCoroutine = null;
        }
    }

    private void CheckEnergyRegeneration()
    {
        if (isFuseBroken)
        {
            StopEnergyRegeneration();
            return;
        }

        if (CurrentEnergy < maxEnergy)
        {
            StartEnergyRegeneration();
        }
    }
    
    private IEnumerator EnergyRegenerateVPercentage()
    {
        while (!isFuseBroken && CurrentEnergy < maxEnergy)
        {
            yield return new WaitForSeconds(timeToRegenerateEnergy);

            if (isFuseBroken)
            {
                _energyRegenerationCoroutine = null;
                yield break;
            }

            CurrentEnergy += GetPercentageToEnergy(energyToRegenPercentage);
            CurrentEnergy = Mathf.Clamp(CurrentEnergy, 0f, maxEnergy);
            Log.Info($"Regenerated: {Mathf.Abs(GetPercentageToEnergy(energyToRegenPercentage))}, Current Energy: {CurrentEnergy} ");
        }

        _energyRegenerationCoroutine = null;
    }

    #endregion

    #region Energy Status

    private void SetEnergyStatus()
    {
        float energyPercentage = GetCurrentEnergyPercentage();
        EnergyStatus previousStatus = _energyStatus;
        
        if (energyPercentage <= 0f)
        {
            _energyStatus = EnergyStatus.Empty;
        }
        else if (energyPercentage <= 20f)
        {
            _energyStatus = EnergyStatus.Low;
        }
        else
        {
            _energyStatus = EnergyStatus.Full;
        }
        if (_energyStatus != previousStatus)
        {
            TriggerEnergyEvents();
        }
    }
    
    private void TriggerEnergyEvents()
    {
        SFXManager.SetState(_energyStatus.ToString(),"Energy_Status");
        GameEventChannel<OnEnergyStatusChange>.RaiseEvent(new OnEnergyStatusChange
        {
            energyStatus = _energyStatus
        });
    }

    #endregion
    
    #region Fuse Logic

    private void CheckFuseOverload()
    {
        if (isFuseBroken)
        {
            return;
        }

        if (energyConsumptionRate >= fuseBreakConsumptionThreshold && _fuseBreakCoroutine == null)
        {
            _fuseBreakCoroutine = StartCoroutine(FuseOverloadSequence());
        }
        else if (energyConsumptionRate < fuseBreakConsumptionThreshold && _fuseBreakCoroutine != null)
        {
            StopCoroutine(_fuseBreakCoroutine);
            _fuseBreakCoroutine = null;
        }
    }

    private IEnumerator FuseOverloadSequence()
    {
        Log.Info($"Fuse overload sequence activated. Consumption: {energyConsumptionRate}A, Fuse Threshold: {fuseBreakConsumptionThreshold}A.");
        yield return new WaitForSeconds(timeTillFuseBreak);
        _fuseBreakCoroutine = null;

        if (energyConsumptionRate >= fuseBreakConsumptionThreshold)
        {
            BurnFuse();
        }
    }
    
    private void BurnFuse()
    {
        if (isFuseBroken)
        {
            return;
        }

        ActivateBlackout(true);
    }

    public void BreakFuseFromPanel()
    {
        if (isFuseBroken)
        {
            return;
        }

        ActivateBlackout(false);
    }

    public void SetFuseBreakConsumptionThreshold(float newThreshold)
    {
        fuseBreakConsumptionThreshold = Mathf.Max(0f, newThreshold);
        Log.Info($"Fuse break consumption threshold set to {fuseBreakConsumptionThreshold}A.");

        if (!isFuseBroken)
        {
            CheckFuseOverload();
        }
    }

    public void ResetFuseBreakConsumptionThreshold()
    {
        SetFuseBreakConsumptionThreshold(_baseFuseBreakConsumptionThreshold);
    }

    public void ApplyFuseBreakConsumptionThresholdMultiplier(float multiplier)
    {
        SetFuseBreakConsumptionThreshold(_baseFuseBreakConsumptionThreshold * Mathf.Max(0f, multiplier));
    }

    private void ActivateBlackout(bool notifyFuseBurned)
    {
        isFuseBroken = true;
        StopEnergyConsumption();
        StopEnergyRegeneration();
        StopDelayedFuseOverloadCheck();
        CurrentEnergy = 0f;
        SetEnergyStatus();
        Log.Info(_energyStatus.ToString());
        
        Log.Info("Fuse burned. Total blackout activated.");

        if (notifyFuseBurned)
        {
            FuseBurned?.Invoke();
        }
    }

    public void RestoreFuse()
    {
        if (!isFuseBroken)
        {
            return;
        }

        isFuseBroken = false;
        StopFuseOverloadCheck();
        StopDelayedFuseOverloadCheck();
        CurrentEnergy = Mathf.Max(CurrentEnergy, GetPercentageToEnergy(startupEnergyPercentageAfterFuseRepair));
        Log.Info("Fuse restored. Energy flow resumed.");
        FuseRestored?.Invoke();

        CheckEnergyConsumption();
        CheckEnergyRegeneration();
        _delayedFuseOverloadCheckCoroutine = StartCoroutine(DelayedFuseOverloadCheck());
    }

    private IEnumerator DelayedFuseOverloadCheck()
    {
        yield return new WaitForSeconds(overloadCheckDelayAfterFuseRepair);
        _delayedFuseOverloadCheckCoroutine = null;
        CheckFuseOverload();
    }

    private void StopFuseOverloadCheck()
    {
        if (_fuseBreakCoroutine != null)
        {
            StopCoroutine(_fuseBreakCoroutine);
            _fuseBreakCoroutine = null;
        }
    }

    private void StopDelayedFuseOverloadCheck()
    {
        if (_delayedFuseOverloadCheckCoroutine != null)
        {
            StopCoroutine(_delayedFuseOverloadCheckCoroutine);
            _delayedFuseOverloadCheckCoroutine = null;
        }
    }

    #endregion
    
    #region InstantEnergyChanges

    private void RestoreEnergy(float amount)
    {
        CurrentEnergy += amount;
    }
    
    private void ConsumeEnergyAmount(float amount)
    {
        CurrentEnergy -= amount;
    }
    
    #endregion

    #region Getters and Utility Methods

    public float GetCurrentEnergy()
    {
        return CurrentEnergy;
    }
    
    public float GetCurrentEnergyPercentage()
    {
        float currentPercentage = CurrentEnergy / maxEnergy * 100f;
        return currentPercentage;
    }

    private float GetPercentageToEnergy(float percentage)
    {
        float energy = (percentage / 100f) * maxEnergy;
        energy = Mathf.Clamp(energy, 0f, maxEnergy);
        return energy;
    }
    
    private float GetEnergyToPercentage(float energy)
    {
        float percentage = (energy / maxEnergy) * 100f;
        percentage = Mathf.Clamp(percentage, 0f, 100f);
        return percentage;
    }
    
    [ContextMenu("Time/Get Total Time")]
    private float TotalTimeInGame()
    {
        if (energyConsumptionRate > 0)
        {
            float time = maxEnergy / energyConsumptionRate;
            return time;
        }
        return Mathf.Infinity;
    }
    
    [ContextMenu("Time/Get Current Time Remaining")]
    private float CurrentTimeRemaining()
    {
        if (energyConsumptionRate > 0)
        {
            float time = CurrentEnergy / energyConsumptionRate;
            return time;
        }
        return Mathf.Infinity;
    }

    #endregion
    
}
