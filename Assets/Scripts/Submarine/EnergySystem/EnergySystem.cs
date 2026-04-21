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
    [SerializeField] private float _currentEnergy; 
    private bool _isEnergyBeingConsumed;
    private bool _isEnergyRegenerating; 
    private Coroutine _energyRegenerationCoroutine;
    private Coroutine _energyConsumptionCoroutine;

    private void Start()
    {
        _currentEnergy = maxEnergy;
    }
    
    [ContextMenu("Consumption/Start Energy Consumption")]
    private void StartEnergyConsumption()
    {
        _isEnergyBeingConsumed = true;
        _energyConsumptionCoroutine ??= StartCoroutine(EnergyDrain());
    }

    [ContextMenu("Consumption/Pause Consumption")]
    private void PauseEnergyConsumption()
    {
        _isEnergyBeingConsumed = false;
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
                _currentEnergy -= energyConsumptionRate * Time.deltaTime;
                _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, maxEnergy);
            }
            yield return null;
        }
        _energyConsumptionCoroutine = null;
        Debug.Log("Energy totally Drained");
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
        while (_currentEnergy < maxEnergy)
        {
            if (_isEnergyRegenerating)
            {
                _currentEnergy += GetPercentageToEnergy(energyToRegen) * Time.deltaTime;
                _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, maxEnergy);
            }
            yield return new WaitForSeconds(timeToRegenerateEnergy);
        }
    }
        
    
    /// <summary>
    /// Fixed amount of energy regeneration and consumption methods
    /// </summary>
    public void RestoreEnergy(float amount)
    {
        if (_currentEnergy < maxEnergy)
        {
            _currentEnergy += amount;
            _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, maxEnergy);
        }
    }
    public void ConsumeEnergyAmount(float amount)
    {
        if (_currentEnergy >= amount)
        {
            _currentEnergy -= amount;
            _currentEnergy = Mathf.Clamp(_currentEnergy, 0f, maxEnergy);
            Debug.Log("Amount To Consume: " + amount);
        }
    }
    
    /// <summary>
    /// Getters for Current Energy and Percentage of Energy
    /// </summary>
    public float GetCurrentEnergy()
    {
        Debug.Log("Current Energy Value = " + _currentEnergy);
        return _currentEnergy;
    }
    
    public float GetCurrentEnergyPercentage()
    {
        float currentPercentage = (_currentEnergy / maxEnergy) * 100f;
        Debug.Log("Current Energy Percentage= " + currentPercentage);
        return currentPercentage;
    }

    private float GetPercentageToEnergy(float percentage)
    {
        float energy = (percentage / 100f) * maxEnergy;
        energy = Mathf.Clamp(energy, 0f, maxEnergy);
        Debug.Log("Energy to Regenerate = " + energy);
        return energy;
    }
    
    private float GetEnergyToPercentage(float energy)
    {
        float percentage = (energy / maxEnergy) * 100f;
        percentage = Mathf.Clamp(percentage, 0f, maxEnergy);
        Debug.Log("Percentage to Regenerate = " + percentage);
        return percentage;
    }
    
    [ContextMenu("Time/Get Total Time")]
    private float TotalTimeInGame()
    {
        if (energyConsumptionRate > 0)
        {
            float time = maxEnergy / energyConsumptionRate;
            Debug.Log("Time Remaining = " + TimeSpan.FromSeconds(time).ToString("mm\\:ss") + " Seconds");
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
            Debug.Log("Time Remaining = " + TimeSpan.FromSeconds(time).ToString("mm\\:ss") + " Seconds");
            return time;
        }
        return Mathf.Infinity;
    }
}
