using System;
using System.Collections;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 5000f;
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
    private bool _isEnergyBeingConsumed;
    
    [Header("Coroutines")]
    private Coroutine _energyRegenerationCoroutine;
    private Coroutine _energyConsumptionCoroutine;
    private Coroutine _energyStressCoroutine;

    private int _stressIndex;
    private float _currentEnergy;
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

    private void OnEnable()
    {
        if (onConsumeEnergy != null) onConsumeEnergy.OnEventRaised += ChangeEnergyValues;
    }
    
    private void OnDisable()
    {
        if (onConsumeEnergy != null) onConsumeEnergy.OnEventRaised -= ChangeEnergyValues;
    }
    
    private void Start()
    {
        CurrentEnergy = maxEnergy;
        SetEnergyStatus();
    }
    
    [ContextMenu("Consumption/Start Energy Consumption")]
    private void StartEnergyConsumption()
    {
        _isEnergyBeingConsumed = true;
        _energyConsumptionCoroutine ??= StartCoroutine(EnergyDrain());
    }

    [ContextMenu("Consumption/Stop Energy Consumption")]
    private void StopEnergyConsumption()
    {
        if (_energyConsumptionCoroutine != null)
        {
            StopCoroutine(_energyConsumptionCoroutine);
            _energyConsumptionCoroutine = null;
        }
        _isEnergyBeingConsumed = false;
    }
    
    private void ChangeEnergyValues(EnergyConsumeData consumeData)
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
        Log.Info($"Energy Consumption Rate Changed: {energyConsumptionRate} - Stress Index: {_stressIndex}");
        CheckStress();
    }

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

    private IEnumerator EnergyDrain()
    {
        while (CurrentEnergy > 0)
        {
            if (_isEnergyBeingConsumed)
            {
                CurrentEnergy -= energyConsumptionRate * Time.deltaTime;
            }
            yield return null;
        }
        _energyConsumptionCoroutine = null;
    }
    
    [ContextMenu("Consumption/Pause Consumption")]
    private void PauseEnergyConsumption()
    {
        _isEnergyBeingConsumed = false;
    }

    #region Energy Regeneration

    [ContextMenu("Regeneration/Start Energy Regeneration")]
    private void StartEnergyRegeneration()
    {
        _energyRegenerationCoroutine ??= StartCoroutine(EnergyRegenerateVPercentage());
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
    
    private IEnumerator EnergyRegenerateVPercentage()
    {
        while (CurrentEnergy < maxEnergy)
        { 
            CurrentEnergy += GetPercentageToEnergy(energyToRegenPercentage);
            yield return new WaitForSeconds(timeToRegenerateEnergy);
        }
        _energyRegenerationCoroutine = null;
    }

    #endregion

    #region Set Energy Status

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
        return _currentEnergy;
    }
    
    public float GetCurrentEnergyPercentage()
    {
        float currentPercentage = (_currentEnergy / maxEnergy) * 100f;
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
            float time = _currentEnergy / energyConsumptionRate;
            return time;
        }
        return Mathf.Infinity;
    }

    #endregion

    private void OnStressAchieved()
    {
        StopEnergyConsumption();
        Log.Info("Stress Achieved, Energy Consumption Stopped");
        //controlPanel?.NotifyFuseBurned();
    }
    
    public void StartConsumption() => StartEnergyConsumption();
    public void StopConsumption() => StopEnergyConsumption();

}
