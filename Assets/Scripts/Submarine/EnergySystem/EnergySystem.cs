using System;
using System.Collections;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 5000f;
    [SerializeField] private float energyToRegen = 5f;
    [SerializeField] private float timeToRegenerateEnergy = 5f;
    [SerializeField] private float energyConsumptionRate = 10f;
    
    [Header("Energy Events Channels")]
    [SerializeField] private EnergyStatusEventSO energyStatusChangeEvent;
    
    [Header("Energy Status")]
    private EnergyStatus _energyStatus;
    private bool _isEnergyBeingConsumed;
    private bool _isEnergyRegenerating;
    
    [Header("Coroutines")]
    private Coroutine _energyRegenerationCoroutine;
    private Coroutine _energyConsumptionCoroutine;
    
    private float _currentEnergy;
    private float CurrentEnergy
    {
        get => _currentEnergy;
        set
        {
            _currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
            SetEnergyStatus();
        }
    }

    private void Start()
    {
        CurrentEnergy = maxEnergy;
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

    private IEnumerator EnergyDrain()
    {
        while (_currentEnergy > 0)
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
    
    [ContextMenu("Regeneration/Start Energy Regeneration")]
    private void StartEnergyRegeneration()
    {
        _isEnergyRegenerating = true;
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
        _isEnergyRegenerating = false;
    }
    
    private IEnumerator EnergyRegenerateVPercentage()
    {
        while (CurrentEnergy < maxEnergy)
        {
            if (_isEnergyRegenerating)
            {
                CurrentEnergy += GetPercentageToEnergy(energyToRegen);
            }
            yield return new WaitForSeconds(timeToRegenerateEnergy);
        }
    }
    
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
        if (energyStatusChangeEvent != null )
        {
            energyStatusChangeEvent.RaiseEvent(_energyStatus);
        }
    }
    
    private void RestoreEnergy(float amount)
    {
        CurrentEnergy += amount;
    }
    
    private void ConsumeEnergyAmount(float amount)
    {
        if (CurrentEnergy >= amount)
        {
            CurrentEnergy -= amount;
        }
    }
    
    private void ExplodeEnergyFuse()
    {
        StopEnergyConsumption();
        //controlPanel?.NotifyFuseBurned();
    }

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
        percentage = Mathf.Clamp(percentage, 0f, maxEnergy);
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

    public void StartConsumption() => StartEnergyConsumption();
    public void StopConsumption() => StopEnergyConsumption();
}
