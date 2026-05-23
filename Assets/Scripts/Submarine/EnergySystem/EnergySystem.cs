using System;
using System.Collections;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 5000f;
    [SerializeField] private float _currentEnergy;
    [SerializeField] private float energyToRegenPercentage = 1f;
    [SerializeField] private float timeToRegenerateEnergy = 5f;
    [SerializeField] private float energyConsumptionRate = 0f;
    
    [Header("Stress Settings")]
    [SerializeField] private int maxStressIndex = 5;
    [SerializeField] private float timeTillStressDamage = 10f;
    
    [Header("Energy Events Channels")]
    [SerializeField] private EnergyStatusEventSO onEnergyStatusChange;
    [SerializeField] private EnergyPropertyEventChannelSO onEnergyPropertyChange;
    [SerializeField] private EnergyToConsumeEventChannelSO onConsumeEnergy;

    [Header("Energy Status")]
    private EnergyStatus _energyStatus;
    
    [Header("Coroutines")]
    private Coroutine _energyRegenerationCoroutine;
    private Coroutine _energyConsumptionCoroutine;
    private Coroutine _energyStressCoroutine;

    private int _stressIndex;
    private float CurrentEnergy
    {
        get => _currentEnergy;
        set
        {
            _currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
            onEnergyPropertyChange?.RaiseEvent(new EnergyProperty { currentEnergyPercentage = GetCurrentEnergyPercentage(), maxEnergyPercentage = 100f });
            SetEnergyStatus();
        }
    }

    #region Initialization

    private void OnEnable()
    {
        if (onConsumeEnergy != null) onConsumeEnergy.OnEventRaised += OnChangeEnergyValues;
    }
    
    private void OnDisable()
    {
        if (onConsumeEnergy != null) onConsumeEnergy.OnEventRaised -= OnChangeEnergyValues;
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

    private void OnChangeEnergyValues(EnergyConsumeData consumeData)
    {
        if (consumeData.isAddingStress)
        {
            energyConsumptionRate += consumeData.energyToConsumeRate;
            _stressIndex++;
        }
        else
        {
            energyConsumptionRate -= consumeData.energyToConsumeRate;
            _stressIndex--;
        }
        CheckEnergyConsumption();
        CheckEnergyRegeneration();
        CheckStress();
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
        Log.Info($"Energy Consumption Rate: {Math.Abs(energyConsumptionRate)} - Stress Index: {_stressIndex}");
    }
    
    private IEnumerator EnergyConsumption()
    {
        while (CurrentEnergy > 0)
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
        }
    }

    private void CheckEnergyRegeneration()
    {
        if (CurrentEnergy < maxEnergy || _energyConsumptionCoroutine != null)
        {
            StartEnergyRegeneration();
        }
    }
    
    private IEnumerator EnergyRegenerateVPercentage()
    {
        while (CurrentEnergy < maxEnergy)
        { 
            CurrentEnergy += GetPercentageToEnergy(energyToRegenPercentage);
            CurrentEnergy = Math.Clamp(CurrentEnergy, 0f, maxEnergy);
            Log.Info($"Regenerated: {Math.Abs(GetPercentageToEnergy(energyToRegenPercentage))}, Current Energy: {CurrentEnergy} ");
            yield return new WaitForSeconds(timeToRegenerateEnergy);
        }
    }

    #endregion

    #region Energy Status

    private void SetEnergyStatus()
    {
        float energyPercentage = GetCurrentEnergyPercentage();
        EnergyStatus previousStatus = _energyStatus;
        
        if (energyPercentage <= 0f) _energyStatus = EnergyStatus.Empty;
        else if (energyPercentage <= 20f) _energyStatus = EnergyStatus.Low;
        else _energyStatus = EnergyStatus.Full;
        
        if (_energyStatus != previousStatus)
        {
            TriggerEnergyEvents();
        }
    }
    
    private void TriggerEnergyEvents()
    {
        if (onEnergyStatusChange != null )
        {
            onEnergyStatusChange.RaiseEvent(_energyStatus);
        }
    }

    #endregion
    
    #region Stress Logic

    private void CheckStress()
    {
        if(_stressIndex >= maxStressIndex && _energyStressCoroutine == null)
        {
            _energyStressCoroutine = StartCoroutine(EnergyStress());
        }
        else if (_stressIndex < maxStressIndex && _energyStressCoroutine != null)
        {
            StopCoroutine(_energyStressCoroutine);
            _energyStressCoroutine = null;
        }
    }

    private IEnumerator EnergyStress()
    {
        Log.Info("Stress Sequence Activated...");
        yield return new WaitForSeconds(timeTillStressDamage);
        OnStressAchieved();
    }
    
    private void OnStressAchieved()
    {
        StopEnergyConsumption();
        Log.Info("Stress Achieved, Energy Consumption Stopped");
        //controlPanel?.NotifyFuseBurned();
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
        float currentPercentage = (CurrentEnergy / maxEnergy) * 100f;
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
